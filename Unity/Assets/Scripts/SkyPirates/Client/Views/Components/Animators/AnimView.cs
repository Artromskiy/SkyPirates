using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SkyPirates.Client.Views.Components.Animators
{
    public readonly struct AnimBlend
    {
        public string ClipHolder { get; }
        public float Weight { get; }

        public AnimBlend(string clipHolder, float weight)
        {
            ClipHolder = clipHolder;
            Weight = weight;
        }
    }

    [DisallowMultipleComponent]
    public sealed class AnimView : MonoBehaviour
    {
        public const string BaseLayer = "Base";
        public const float DefaultFadeSeconds = 0.1f;

        [SerializeField] private AnimSet _animationSet = null!;

        private readonly Dictionary<string, AnimLayer> _layers = new Dictionary<string, AnimLayer>(StringComparer.Ordinal);
        private readonly Dictionary<string, ClipHolderReference> _clipHolders = new Dictionary<string, ClipHolderReference>(StringComparer.Ordinal);
        private readonly Dictionary<string, AnimBlendGroupReference> _blendGroups = new Dictionary<string, AnimBlendGroupReference>(StringComparer.Ordinal);
        private PlayableGraph _graph;
        private AnimationLayerMixerPlayable _layerMixer;
        private Animator _animator;
        private RuntimeAnimatorController _previousController = null!;
        private bool _controllerWasCleared;
        private bool _graphCreated;
        private bool _graphRebuildRequired;

        public Animator Animator
        {
            get
            {
                if (_animator == null)
                    _animator = GetComponentInChildren<Animator>();

                return _animator;
            }
        }
        public bool IsReady => _graphCreated;
        public IReadOnlyCollection<string> LayerNames => _layers.Keys;

        public bool Play(string clipHolder, float fadeSeconds = DefaultFadeSeconds, bool restart = true)
        {
            if (!TryGetClipHolder(clipHolder, out ClipHolderReference holder)
                || !holder.Layer.Play(holder.Index, fadeSeconds, restart))
                return false;

            holder.Layer.FadeWeight(1f, fadeSeconds);
            return true;
        }

        public bool PlayOnce(string clipHolder, float fadeSeconds = DefaultFadeSeconds)
        {
            if (!TryGetClipHolder(clipHolder, out ClipHolderReference holder)
                || !holder.Layer.Play(holder.Index, fadeSeconds, true)
                || !holder.Layer.StopAfterClip(holder.Index, fadeSeconds))
                return false;

            holder.Layer.FadeWeight(1f, fadeSeconds);
            return true;
        }

        public bool Blend(string blendGroupName, AnimBlend[] blend, float fadeSeconds = DefaultFadeSeconds)
        {
            if (!TryGetBlendGroup(blendGroupName, AnimBlendMode.Weighted, out AnimBlendGroupReference group)
                || blend == null
                || !group.Layer.Blend(blend, fadeSeconds, group.HolderIndices))
                return false;

            group.Layer.FadeWeight(1f, fadeSeconds);
            return true;
        }

        public bool Blend2D(string blendGroupName, Vector2 parameter, float fadeSeconds = DefaultFadeSeconds)
        {
            if (!TryGetBlendGroup(blendGroupName, AnimBlendMode.Directional2D, out AnimBlendGroupReference group))
                return false;

            group.CalculateDirectionalWeights(parameter);
            group.SynchronizeDirectionalPlayback();
            group.Layer.SetWeights(group.HolderIndicesInPointOrder, group.Weights, fadeSeconds);
            group.Layer.FadeWeight(1f, fadeSeconds);
            return true;
        }

        public bool SetClipHolderWeight(string clipHolder, float weight, float fadeSeconds = DefaultFadeSeconds)
        {
            if (!TryGetClipHolder(clipHolder, out ClipHolderReference holder)
                || !holder.Layer.SetClipHolderWeight(holder.Index, weight, fadeSeconds))
                return false;

            holder.Layer.FadeWeight(1f, fadeSeconds);
            return true;
        }

        public bool SetClipHolderTime(string clipHolder, float normalizedTime)
        {
            return TryGetClipHolder(clipHolder, out ClipHolderReference holder)
                   && holder.Layer.SetClipHolderTime(holder.Index, normalizedTime);
        }

        public bool SetClipHolderSpeed(string clipHolder, float speed)
        {
            return TryGetClipHolder(clipHolder, out ClipHolderReference holder)
                   && holder.Layer.SetClipHolderSpeed(holder.Index, speed);
        }

        public bool FadeLayer(string layerName, float targetWeight, float fadeSeconds = DefaultFadeSeconds)
        {
            if (!TryGetLayer(layerName, out AnimLayer layer))
                return false;

            layer.FadeWeight(targetWeight, fadeSeconds);
            return true;
        }

        public bool SetLayerWeight(string layerName, float weight)
        {
            return FadeLayer(layerName, weight, 0f);
        }

        public bool StopLayer(string layerName, float fadeSeconds = DefaultFadeSeconds)
        {
            if (!TryGetLayer(layerName, out AnimLayer layer))
                return false;

            layer.Clear(fadeSeconds);
            if (!string.Equals(layerName, BaseLayer, StringComparison.Ordinal))
                layer.FadeWeight(0f, fadeSeconds);
            return true;
        }

        private void OnEnable()
        {
            AnimAssetChangeNotifier.Changed += OnAnimAssetChanged;
            _graphRebuildRequired = false;
            CreateGraph();
        }

        private void Update()
        {
            if (_graphRebuildRequired)
            {
                _graphRebuildRequired = false;
                DestroyGraph();
                if (isActiveAndEnabled)
                    CreateGraph();
            }

            if (!_graphCreated)
                return;

            foreach (AnimLayer layer in _layers.Values)
                layer.Update(Time.deltaTime);
        }

        private void OnDisable()
        {
            AnimAssetChangeNotifier.Changed -= OnAnimAssetChanged;
            DestroyGraph();
        }

        private void OnDestroy()
        {
            AnimAssetChangeNotifier.Changed -= OnAnimAssetChanged;
            DestroyGraph();
        }

        private void OnValidate()
        {
            _graphRebuildRequired = true;
        }

        private void OnAnimAssetChanged(ScriptableObject asset)
        {
            if (asset == _animationSet || (_animationSet != null && asset == _animationSet.StructureProfile))
                _graphRebuildRequired = true;
        }

        private void CreateGraph()
        {
            if (_graphCreated)
                return;

            Animator animator = Animator;
            if (animator == null || _animationSet == null || _animationSet.StructureProfile == null)
            {
                Debug.LogError("AnimView requires an Animator and an AnimSet with an AnimProfile assigned.", this);
                return;
            }

            _previousController = animator.runtimeAnimatorController;
            _controllerWasCleared = _previousController != null;
            if (_controllerWasCleared)
                animator.runtimeAnimatorController = null;

            _graph = PlayableGraph.Create(name + " Animation Graph");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _layerMixer = AnimationLayerMixerPlayable.Create(_graph, 1);

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Anim Output", animator);
            output.SetSourcePlayable(_layerMixer);

            LoadProfile();
            _graph.Play();
            _graphCreated = true;
        }

        private void LoadProfile()
        {
            Dictionary<string, AnimClipDefinition> animationClips = new Dictionary<string, AnimClipDefinition>(StringComparer.Ordinal);
            AnimClipDefinition[] clips = _animationSet.Clips;
            if (clips != null)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    AnimClipDefinition clip = clips[i];
                    if (clip == null || !IsValidName(clip.ClipHolder) || clip.Clip == null)
                        continue;

                    if (!animationClips.TryAdd(clip.ClipHolder, clip))
                        Debug.LogWarning("Duplicate animation assignment for clip holder '" + clip.ClipHolder + "'.", this);
                }
            }

            AnimLayer baseLayer = CreateLayerInternal(BaseLayer, null!, false, 1f);
            _layers.Add(BaseLayer, baseLayer);

            AnimProfile structureProfile = _animationSet.StructureProfile;
            AnimLayerDefinition[] layerDefinitions = structureProfile.Layers;
            if (layerDefinitions != null)
            {
                for (int i = 0; i < layerDefinitions.Length; i++)
                {
                    AnimLayerDefinition definition = layerDefinitions[i];
                    if (!IsValidDefinition(definition))
                        continue;

                    if (_layers.TryGetValue(definition.Name, out AnimLayer existing))
                    {
                        existing.SetAdditive(definition.Additive);
                        if (definition.Mask != null)
                            existing.SetMask(definition.Mask);
                        existing.FadeWeight(definition.DefaultWeight, 0f);
                        continue;
                    }

                    AnimLayer layer = CreateLayerInternal(
                        definition.Name,
                        definition.Mask,
                        definition.Additive,
                        definition.DefaultWeight);
                    _layers.Add(definition.Name, layer);
                }
            }

            AnimClipHolderDefinition[] holders = structureProfile.ClipHolders;
            if (holders != null)
            {
                for (int i = 0; i < holders.Length; i++)
                {
                    AnimClipHolderDefinition holder = holders[i];
                    if (!IsValidDefinition(holder))
                        continue;

                    if (!animationClips.TryGetValue(holder.Name, out AnimClipDefinition clipDefinition))
                    {
                        Debug.LogWarning("AnimSet has no clip assigned to holder '" + holder.Name + "'.", this);
                        continue;
                    }

                    if (!_layers.TryGetValue(holder.Layer, out AnimLayer layer))
                    {
                        Debug.LogWarning("Clip holder '" + holder.Name + "' references unknown layer '" + holder.Layer + "'.", this);
                        continue;
                    }

                    if (_clipHolders.ContainsKey(holder.Name))
                    {
                        Debug.LogWarning("Duplicate clip holder name '" + holder.Name + "' in profile.", this);
                        continue;
                    }

                    int holderIndex = layer.AddClipHolder(holder.Name, clipDefinition.Clip, clipDefinition.DefaultSpeed);
                    if (holderIndex < 0)
                        Debug.LogWarning("Duplicate clip holder '" + holder.Name + "' in layer '" + holder.Layer + "'.", this);
                    else
                        _clipHolders.Add(holder.Name, new ClipHolderReference(layer, holderIndex));
                }
            }

            LoadBlendGroups(structureProfile.BlendGroups);
        }

        private void LoadBlendGroups(AnimBlendGroupDefinition[] definitions)
        {
            if (definitions == null)
                return;

            for (int i = 0; i < definitions.Length; i++)
            {
                AnimBlendGroupDefinition definition = definitions[i];
                if (!IsValidDefinition(definition))
                    continue;

                if (_blendGroups.ContainsKey(definition.Name))
                {
                    Debug.LogWarning("Duplicate animation blend group '" + definition.Name + "' in profile.", this);
                    continue;
                }

                if (!_layers.TryGetValue(definition.Layer, out AnimLayer layer))
                {
                    Debug.LogWarning("Blend group '" + definition.Name + "' references unknown layer '" + definition.Layer + "'.", this);
                    continue;
                }

                AnimBlendPointDefinition[] points = definition.Points;
                if (points == null || points.Length == 0)
                {
                    Debug.LogWarning("Blend group '" + definition.Name + "' has no points.", this);
                    continue;
                }

                List<AnimBlendPointReference> runtimePoints = new List<AnimBlendPointReference>(points.Length);
                HashSet<int> holderIndices = new HashSet<int>();
                bool invalid = false;
                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                {
                    AnimBlendPointDefinition point = points[pointIndex];
                    if (point == null || !_clipHolders.TryGetValue(point.ClipHolder, out ClipHolderReference holder)
                        || holder.Layer != layer || !holderIndices.Add(holder.Index))
                    {
                        invalid = true;
                        break;
                    }

                    runtimePoints.Add(new AnimBlendPointReference(holder.Index, point.Position));
                }

                if (invalid || (definition.Mode == AnimBlendMode.Directional2D && !HasDirectionalCoverage(runtimePoints)))
                {
                    Debug.LogWarning("Blend group '" + definition.Name + "' has invalid points. Ensure each holder belongs to its layer, is unique, and Directional2D has at least two directions.", this);
                    continue;
                }

                AnimBlendGroupReference group = new AnimBlendGroupReference(
                    definition.Name, layer, definition.Mode, runtimePoints.ToArray(), holderIndices);
                _blendGroups.Add(definition.Name, group);
            }
        }

        private void DestroyGraph()
        {
            if (_graph.IsValid())
                _graph.Destroy();

            Animator animator = Animator;
            if (_controllerWasCleared && animator != null && animator.runtimeAnimatorController == null)
                animator.runtimeAnimatorController = _previousController;

            _layers.Clear();
            _clipHolders.Clear();
            _blendGroups.Clear();
            _graphCreated = false;
            _controllerWasCleared = false;
            _previousController = null!;
        }

        private sealed class ClipHolderReference
        {
            public readonly AnimLayer Layer;
            public readonly int Index;

            public ClipHolderReference(AnimLayer layer, int index)
            {
                Layer = layer;
                Index = index;
            }
        }

        private readonly struct AnimBlendPointReference
        {
            public readonly int HolderIndex;
            public readonly Vector2 Position;

            public AnimBlendPointReference(int holderIndex, Vector2 position)
            {
                HolderIndex = holderIndex;
                Position = position;
            }
        }

        private sealed class AnimBlendGroupReference
        {
            public readonly string Name;
            public readonly AnimLayer Layer;
            public readonly AnimBlendMode Mode;
            public readonly HashSet<int> HolderIndices;
            public readonly float[] Weights;

            private readonly float[] _directionAngles;
            private readonly float[] _pointNormalizedSpeeds;
            private readonly int[] _directionPointIndices;
            private readonly int[] _holderIndicesInPointOrder;
            private readonly int _centerPointIndex;
            private readonly float _maxRadius;
            private float _normalizedSpeed;
            private float _lastSynchronizedSpeed;
            private bool _directionalPlaybackSynchronized;
            private bool _hasSynchronizedSpeed;
            private Vector2 _lastParameter;
            private bool _hasCachedWeights;

            public int[] HolderIndicesInPointOrder => _holderIndicesInPointOrder;

            public AnimBlendGroupReference(string name, AnimLayer layer, AnimBlendMode mode,
                AnimBlendPointReference[] points, HashSet<int> holderIndices)
            {
                Name = name;
                Layer = layer;
                Mode = mode;
                HolderIndices = holderIndices;
                Weights = new float[points.Length];
                _pointNormalizedSpeeds = new float[points.Length];
                _holderIndicesInPointOrder = new int[points.Length];
                _centerPointIndex = -1;
                float maxRadius = 0f;
                int directionCount = 0;

                for (int i = 0; i < points.Length; i++)
                {
                    if (IsDirectionalPoint(points[i].Position))
                        directionCount++;
                }

                _directionAngles = new float[directionCount];
                _directionPointIndices = new int[directionCount];
                int directionIndex = 0;

                for (int i = 0; i < points.Length; i++)
                {
                    _holderIndicesInPointOrder[i] = points[i].HolderIndex;
                    _pointNormalizedSpeeds[i] = layer.GetDefaultNormalizedSpeed(points[i].HolderIndex);
                    Vector2 position = points[i].Position;
                    float radius = position.magnitude;
                    if (radius <= 0.0001f)
                    {
                        _centerPointIndex = i;
                    }
                    else if (radius > maxRadius)
                        maxRadius = radius;

                    if (IsDirectionalPoint(position))
                    {
                        _directionAngles[directionIndex] = Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg;
                        _directionPointIndices[directionIndex] = i;
                        directionIndex++;
                    }
                }

                _maxRadius = maxRadius;
            }

            public void CalculateDirectionalWeights(Vector2 parameter)
            {
                if (_hasCachedWeights && parameter.Equals(_lastParameter))
                    return;

                _lastParameter = parameter;
                _hasCachedWeights = true;
                Array.Clear(Weights, 0, Weights.Length);
                if (_directionPointIndices.Length == 0)
                {
                    ApplyCenterOnlyWeight();
                    return;
                }

                float magnitude = parameter.magnitude;
                if (TrySetCenterWeight(magnitude))
                    return;

                if (magnitude <= 0.0001f)
                    parameter = Vector2.right;

                int lowerDirection = -1;
                int upperDirection = -1;
                float lowerDelta = float.NegativeInfinity;
                float upperDelta = float.PositiveInfinity;
                FindDirectionPair(parameter, ref lowerDirection, ref upperDirection, ref lowerDelta, ref upperDelta);
                CompleteDirectionPair(ref lowerDirection, ref upperDirection, ref lowerDelta, ref upperDelta);
                ApplyDirectionalWeights(magnitude, lowerDirection, upperDirection, lowerDelta, upperDelta);
            }

            public void SynchronizeDirectionalPlayback()
            {
                if (_directionPointIndices.Length == 0)
                    return;

                if (!_directionalPlaybackSynchronized)
                {
                    SynchronizeFirstPlayback();
                    return;
                }

                if (_hasSynchronizedSpeed && _lastSynchronizedSpeed == _normalizedSpeed)
                    return;

                UpdateDirectionalPlaybackSpeed();
                _lastSynchronizedSpeed = _normalizedSpeed;
                _hasSynchronizedSpeed = true;
            }

            private bool TrySetCenterWeight(float magnitude)
            {
                if (magnitude > 0.0001f || _centerPointIndex < 0)
                    return false;

                Weights[_centerPointIndex] = 1f;
                CalculateWeightedNormalizedSpeed();
                return true;
            }

            private void ApplyCenterOnlyWeight()
            {
                if (_centerPointIndex >= 0)
                    Weights[_centerPointIndex] = 1f;

                CalculateWeightedNormalizedSpeed();
            }

            private void FindDirectionPair(Vector2 parameter, ref int lowerDirection, ref int upperDirection,
                ref float lowerDelta, ref float upperDelta)
            {
                float angle = Mathf.Atan2(parameter.y, parameter.x) * Mathf.Rad2Deg;
                for (int i = 0; i < _directionAngles.Length; i++)
                {
                    float delta = Mathf.DeltaAngle(angle, _directionAngles[i]);
                    if (delta <= 0f && delta > lowerDelta)
                    {
                        lowerDelta = delta;
                        lowerDirection = i;
                    }
                    if (delta >= 0f && delta < upperDelta)
                    {
                        upperDelta = delta;
                        upperDirection = i;
                    }
                }
            }

            private void CompleteDirectionPair(ref int lowerDirection, ref int upperDirection,
                ref float lowerDelta, ref float upperDelta)
            {
                if (lowerDirection < 0)
                {
                    lowerDirection = upperDirection;
                    lowerDelta = upperDelta - 360f;
                }
                if (upperDirection < 0)
                {
                    upperDirection = lowerDirection;
                    upperDelta = lowerDelta + 360f;
                }
            }

            private void ApplyDirectionalWeights(float magnitude, int lowerDirection, int upperDirection,
                float lowerDelta, float upperDelta)
            {
                float directionalWeight = GetDirectionalWeight(magnitude);
                ApplyCenterWeight(directionalWeight);
                float span = upperDelta - lowerDelta;
                float upperWeight = GetUpperWeight(span, lowerDelta);
                int lowerPoint = _directionPointIndices[lowerDirection];
                int upperPoint = _directionPointIndices[upperDirection];
                float lowerWeight = directionalWeight * (1f - upperWeight);
                float upperDirectionalWeight = directionalWeight * upperWeight;
                Weights[lowerPoint] += lowerWeight;
                Weights[upperPoint] += upperDirectionalWeight;
                CalculateWeightedNormalizedSpeed();
            }

            private float GetDirectionalWeight(float magnitude)
            {
                if (_centerPointIndex < 0)
                    return 1f;

                return Mathf.Clamp01(magnitude / Mathf.Max(_maxRadius, 0.0001f));
            }

            private void ApplyCenterWeight(float directionalWeight)
            {
                if (_centerPointIndex < 0)
                    return;

                Weights[_centerPointIndex] = 1f - directionalWeight;
            }

            private void CalculateWeightedNormalizedSpeed()
            {
                _normalizedSpeed = 0f;
                for (int i = 0; i < Weights.Length; i++)
                    _normalizedSpeed += Weights[i] * _pointNormalizedSpeeds[i];
            }

            private static float GetUpperWeight(float span, float lowerDelta)
            {
                if (span <= 0.0001f)
                    return 0f;

                return Mathf.Clamp01(-lowerDelta / span);
            }

            private void SynchronizeFirstPlayback()
            {
                int referencePoint = _centerPointIndex >= 0 ? _centerPointIndex : _directionPointIndices[0];
                int referenceIndex = _holderIndicesInPointOrder[referencePoint];
                float normalizedTime = Layer.GetNormalizedTime(referenceIndex);
                for (int i = 0; i < _holderIndicesInPointOrder.Length; i++)
                {
                    int holderIndex = _holderIndicesInPointOrder[i];
                    Layer.SetClipHolderTime(holderIndex, normalizedTime);
                    Layer.SetNormalizedSpeed(holderIndex, _normalizedSpeed);
                }

                _lastSynchronizedSpeed = _normalizedSpeed;
                _hasSynchronizedSpeed = true;
                _directionalPlaybackSynchronized = true;
            }

            private void UpdateDirectionalPlaybackSpeed()
            {
                for (int i = 0; i < _holderIndicesInPointOrder.Length; i++)
                {
                    int holderIndex = _holderIndicesInPointOrder[i];
                    Layer.SetNormalizedSpeed(holderIndex, _normalizedSpeed);
                }
            }
        }

        private sealed class AnimLayer
        {
            private readonly AnimationMixerPlayable _mixer;
            private readonly AnimationLayerMixerPlayable _layerMixer;
            private readonly int _layerIndex;
            private readonly Dictionary<string, int> _holderIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly List<AnimationClipPlayable> _holders = new List<AnimationClipPlayable>();
            private readonly List<float> _defaultSpeeds = new List<float>();
            private readonly List<float> _clipLengths = new List<float>();
            private readonly List<float> _defaultNormalizedSpeeds = new List<float>();
            private float[] _currentWeights = Array.Empty<float>();
            private float[] _targetWeights = Array.Empty<float>();
            private float _weightFadeDuration;
            private float _layerWeight;
            private float _targetLayerWeight;
            private float _layerFadeDuration;
            private float _autoStopRemaining = -1f;
            private float _autoStopFadeDuration;

            public AnimLayer(AnimationMixerPlayable mixer, AnimationLayerMixerPlayable layerMixer, int layerIndex, float weight)
            {
                _mixer = mixer;
                _layerMixer = layerMixer;
                _layerIndex = layerIndex;
                _layerWeight = Mathf.Clamp01(weight);
                _targetLayerWeight = _layerWeight;
            }

            public int AddClipHolder(string name, AnimationClip clip, float defaultSpeed)
            {
                if (_holderIndices.ContainsKey(name))
                    return -1;

                int index = _holders.Count;
                _mixer.SetInputCount(index + 1);

                AnimationClipPlayable playable = AnimationClipPlayable.Create(_mixer.GetGraph(), clip);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                playable.SetTime(0d);
                playable.SetSpeed(defaultSpeed);
                _mixer.GetGraph().Connect(playable, 0, _mixer, index);
                _mixer.SetInputWeight(index, 0f);

                _holders.Add(playable);
                _defaultSpeeds.Add(defaultSpeed);
                _clipLengths.Add(clip.length);
                _defaultNormalizedSpeeds.Add(clip.length <= Mathf.Epsilon ? 0f : defaultSpeed / clip.length);
                _holderIndices.Add(name, index);
                Array.Resize(ref _currentWeights, index + 1);
                Array.Resize(ref _targetWeights, index + 1);
                return index;
            }

            public bool Play(int index, float fadeSeconds, bool restart)
            {
                if (!IsValidHolderIndex(index))
                    return false;

                _autoStopRemaining = -1f;
                if (restart)
                {
                    _holders[index].SetTime(0d);
                    _holders[index].SetSpeed(_defaultSpeeds[index]);
                }

                Array.Clear(_targetWeights, 0, _targetWeights.Length);
                _targetWeights[index] = 1f;
                BeginWeightFade(fadeSeconds);
                ApplyWeightFadeImmediately();
                return true;
            }

            public bool StopAfterClip(int index, float fadeSeconds)
            {
                if (!IsValidHolderIndex(index))
                    return false;

                AnimationClipPlayable playable = _holders[index];
                float speed = (float)playable.GetSpeed();
                if (speed <= Mathf.Epsilon)
                    return false;

                _autoStopRemaining = _clipLengths[index] / speed;
                _autoStopFadeDuration = Mathf.Max(0f, fadeSeconds);
                return true;
            }

            public bool Blend(AnimBlend[] blend, float fadeSeconds, HashSet<int> allowedIndices = null!)
            {
                float total = 0f;
                for (int i = 0; i < blend.Length; i++)
                {
                    AnimBlend item = blend[i];
                    if (!TryGetBlendTarget(item, allowedIndices, out _))
                        continue;

                    total += item.Weight;
                }

                if (total <= Mathf.Epsilon || float.IsInfinity(total))
                    return false;

                Array.Clear(_targetWeights, 0, _targetWeights.Length);
                for (int i = 0; i < blend.Length; i++)
                {
                    AnimBlend item = blend[i];
                    if (!TryGetBlendTarget(item, allowedIndices, out int index))
                        continue;

                    _targetWeights[index] += item.Weight / total;
                }

                BeginWeightFade(fadeSeconds);
                ApplyWeightFadeImmediately();
                return true;
            }

            public void SetWeights(int[] indices, float[] weights, float fadeSeconds)
            {
                Array.Clear(_targetWeights, 0, _targetWeights.Length);
                for (int i = 0; i < indices.Length; i++)
                {
                    int index = indices[i];
                    if (index >= 0 && index < _targetWeights.Length)
                        _targetWeights[index] += Mathf.Max(0f, weights[i]);
                }

                BeginWeightFade(fadeSeconds);
                ApplyWeightFadeImmediately();
            }

            public bool SetClipHolderWeight(int index, float weight, float fadeSeconds)
            {
                if (!IsValidHolderIndex(index))
                    return false;

                _targetWeights[index] = Mathf.Clamp01(weight);
                BeginWeightFade(fadeSeconds);
                ApplyWeightFadeImmediately();
                return true;
            }

            public bool SetClipHolderTime(int index, float normalizedTime)
            {
                if (!IsValidHolderIndex(index))
                    return false;

                AnimationClipPlayable playable = _holders[index];
                playable.SetTime(Mathf.Clamp01(normalizedTime) * _clipLengths[index]);
                return true;
            }

            public float GetNormalizedTime(int index)
            {
                if (!IsValidHolderIndex(index))
                    return 0f;

                float clipLength = _clipLengths[index];
                return clipLength <= Mathf.Epsilon
                    ? 0f
                    : Mathf.Repeat((float)_holders[index].GetTime() / clipLength, 1f);
            }

            public float GetDefaultNormalizedSpeed(int index)
            {
                if (!IsValidHolderIndex(index))
                    return 0f;

                return _defaultNormalizedSpeeds[index];
            }

            public void SetNormalizedSpeed(int index, float normalizedSpeed)
            {
                if (!IsValidHolderIndex(index))
                    return;

                AnimationClipPlayable playable = _holders[index];
                playable.SetSpeed(Mathf.Max(0f, normalizedSpeed) * _clipLengths[index]);
            }

            public bool SetClipHolderSpeed(int index, float speed)
            {
                if (!IsValidHolderIndex(index))
                    return false;

                _holders[index].SetSpeed(speed);
                return true;
            }

            public void FadeWeight(float target, float fadeSeconds)
            {
                _targetLayerWeight = Mathf.Clamp01(target);
                _layerFadeDuration = Mathf.Max(0f, fadeSeconds);
                if (_layerFadeDuration == 0f)
                {
                    _layerWeight = _targetLayerWeight;
                    _layerMixer.SetInputWeight(_layerIndex, _layerWeight);
                }
            }

            public void SetAdditive(bool additive)
            {
                _layerMixer.SetLayerAdditive((uint)_layerIndex, additive);
            }

            public void SetMask(AvatarMask mask)
            {
                _layerMixer.SetLayerMaskFromAvatarMask((uint)_layerIndex, mask);
            }

            public void Clear(float fadeSeconds)
            {
                _autoStopRemaining = -1f;
                Array.Clear(_targetWeights, 0, _targetWeights.Length);
                BeginWeightFade(fadeSeconds);
                ApplyWeightFadeImmediately();
            }

            public void Update(float deltaTime)
            {
                UpdateClipWeights(deltaTime);
                UpdateAutoStop(deltaTime);
                if (_layerWeight != _targetLayerWeight)
                {
                    _layerWeight = MoveTowards(_layerWeight, _targetLayerWeight, deltaTime, _layerFadeDuration);
                    _layerMixer.SetInputWeight(_layerIndex, _layerWeight);
                }
            }

            private void UpdateAutoStop(float deltaTime)
            {
                if (_autoStopRemaining < 0f)
                    return;

                _autoStopRemaining -= deltaTime;
                if (_autoStopRemaining > 0f)
                    return;

                _autoStopRemaining = -1f;
                Clear(_autoStopFadeDuration);
                FadeWeight(0f, _autoStopFadeDuration);
            }

            private bool TryGetHolder(string name, out int index)
            {
                if (!IsValidName(name))
                {
                    index = -1;
                    return false;
                }

                return _holderIndices.TryGetValue(name, out index);
            }

            private bool TryGetBlendTarget(AnimBlend blend, HashSet<int> allowedIndices, out int index)
            {
                if (!IsPositiveFinite(blend.Weight)
                    || !TryGetHolder(blend.ClipHolder, out index)
                    || (allowedIndices != null && !allowedIndices.Contains(index)))
                {
                    index = -1;
                    return false;
                }

                return true;
            }

            private bool IsValidHolderIndex(int index)
            {
                return index >= 0 && index < _holders.Count;
            }

            private void ApplyWeightFadeImmediately()
            {
                if (_weightFadeDuration == 0f)
                    UpdateClipWeights(0f);
            }

            private static bool IsPositiveFinite(float value)
            {
                return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
            }

            private void BeginWeightFade(float fadeSeconds)
            {
                _weightFadeDuration = Mathf.Max(0f, fadeSeconds);
            }

            private void UpdateClipWeights(float deltaTime)
            {
                for (int i = 0; i < _currentWeights.Length; i++)
                {
                    if (_currentWeights[i] != _targetWeights[i])
                    {
                        _currentWeights[i] = MoveTowards(_currentWeights[i], _targetWeights[i], deltaTime, _weightFadeDuration);
                        _mixer.SetInputWeight(i, _currentWeights[i]);
                    }
                }
            }

            private static float MoveTowards(float current, float target, float deltaTime, float duration)
            {
                if (duration <= 0f)
                    return target;

                return Mathf.MoveTowards(current, target, deltaTime / duration);
            }
        }

        private static bool HasDirectionalCoverage(List<AnimBlendPointReference> points)
        {
            int directions = 0;
            for (int i = 0; i < points.Count; i++)
            {
                if (IsDirectionalPoint(points[i].Position))
                    directions++;
            }

            return directions >= 2;
        }

        private static bool IsDirectionalPoint(Vector2 position)
        {
            return position.sqrMagnitude > 0.0001f;
        }

        private static bool IsValidDefinition(AnimLayerDefinition definition)
        {
            return definition != null && IsValidName(definition.Name);
        }

        private static bool IsValidDefinition(AnimClipHolderDefinition definition)
        {
            return definition != null
                   && IsValidName(definition.Layer) && IsValidName(definition.Name);
        }

        private static bool IsValidDefinition(AnimBlendGroupDefinition definition)
        {
            return definition != null
                   && IsValidName(definition.Name)
                   && IsValidName(definition.Layer)
                   && Enum.IsDefined(typeof(AnimBlendMode), definition.Mode);
        }

        private AnimLayer CreateLayerInternal(string layerName, AvatarMask mask, bool additive, float weight)
        {
            int layerIndex = _layers.Count;
            _layerMixer.SetInputCount(layerIndex + 1);

            AnimationMixerPlayable mixer = AnimationMixerPlayable.Create(_graph, 1);
            _graph.Connect(mixer, 0, _layerMixer, layerIndex);
            _layerMixer.SetInputWeight(layerIndex, Mathf.Clamp01(weight));
            _layerMixer.SetLayerAdditive((uint)layerIndex, additive);
            if (mask != null)
                _layerMixer.SetLayerMaskFromAvatarMask((uint)layerIndex, mask);

            return new AnimLayer(mixer, _layerMixer, layerIndex, weight);
        }

        private bool TryGetLayer(string layerName, out AnimLayer layer)
        {
            if (!IsReadyForName(layerName))
            {
                layer = null!;
                return false;
            }

            return _layers.TryGetValue(layerName, out layer);
        }

        private bool TryGetClipHolder(string name, out ClipHolderReference holder)
        {
            if (!IsReadyForName(name))
            {
                holder = null!;
                return false;
            }

            return _clipHolders.TryGetValue(name, out holder);
        }

        private bool TryGetBlendGroup(string name, AnimBlendMode mode, out AnimBlendGroupReference group)
        {
            if (!IsReadyForName(name)
                || !_blendGroups.TryGetValue(name, out group)
                || group.Mode != mode)
            {
                group = null!;
                return false;
            }

            return true;
        }

        private bool IsReadyForName(string name)
        {
            return _graphCreated && IsValidName(name);
        }

        private static bool IsValidName(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
