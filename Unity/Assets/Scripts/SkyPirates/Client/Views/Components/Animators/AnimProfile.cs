using System;
using UnityEngine;

namespace SkyPirates.Client.Views.Components.Animators
{
    internal static class AnimAssetChangeNotifier
    {
        public static event Action<ScriptableObject> Changed;

        public static void NotifyChanged(ScriptableObject asset)
        {
            Changed?.Invoke(asset);
        }
    }

    public enum AnimBlendMode
    {
        Weighted,
        Directional2D
    }

    [Serializable]
    public sealed class AnimLayerDefinition
    {
        [SerializeField] private string _name = string.Empty;
        [SerializeField] private AvatarMask _mask = null!;
        [SerializeField] private bool _additive;
        [SerializeField, Range(0f, 1f)] private float _defaultWeight;

        internal string Name => _name;
        internal AvatarMask Mask => _mask;
        internal bool Additive => _additive;
        internal float DefaultWeight => _defaultWeight;
    }

    [Serializable]
    public sealed class AnimClipHolderDefinition
    {
        [SerializeField] private string _layer = AnimView.BaseLayer;
        [SerializeField] private string _name = string.Empty;

        internal string Layer => _layer;
        internal string Name => _name;
    }

    [Serializable]
    public sealed class AnimBlendPointDefinition
    {
        [SerializeField] private string _clipHolder = string.Empty;
        [SerializeField] private Vector2 _position;

        internal string ClipHolder => _clipHolder;
        internal Vector2 Position => _position;
    }

    [Serializable]
    public sealed class AnimBlendGroupDefinition
    {
        [SerializeField] private string _name = string.Empty;
        [SerializeField] private string _layer = AnimView.BaseLayer;
        [SerializeField] private AnimBlendMode _mode;
        [SerializeField] private AnimBlendPointDefinition[] _points = Array.Empty<AnimBlendPointDefinition>();

        internal string Name => _name;
        internal string Layer => _layer;
        internal AnimBlendMode Mode => _mode;
        internal AnimBlendPointDefinition[] Points => _points;
    }

    [CreateAssetMenu(menuName = "Sky Pirates/Anim/Structure Profile", fileName = "AnimProfile")]
    public sealed class AnimProfile : ScriptableObject
    {
        [SerializeField] private AnimLayerDefinition[] _layers = Array.Empty<AnimLayerDefinition>();
        [SerializeField] private AnimClipHolderDefinition[] _clipHolders = Array.Empty<AnimClipHolderDefinition>();
        [SerializeField] private AnimBlendGroupDefinition[] _blendGroups = Array.Empty<AnimBlendGroupDefinition>();

        internal AnimLayerDefinition[] Layers => _layers;
        internal AnimClipHolderDefinition[] ClipHolders => _clipHolders;
        internal AnimBlendGroupDefinition[] BlendGroups => _blendGroups;

        private void OnValidate()
        {
            AnimAssetChangeNotifier.NotifyChanged(this);
        }
    }
}
