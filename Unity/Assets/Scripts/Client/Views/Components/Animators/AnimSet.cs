using System;
using UnityEngine;

namespace SlyPirates.Client.Views.Components.Animators
{
    [Serializable]
    public sealed class AnimClipDefinition
    {
        [SerializeField] private string _clipHolder = string.Empty;
        [SerializeField] private AnimationClip _clip = null!;
        [SerializeField] private float _defaultSpeed = 1f;

        internal string ClipHolder => _clipHolder;
        internal AnimationClip Clip => _clip;
        internal float DefaultSpeed => _defaultSpeed;
    }

    [CreateAssetMenu(menuName = "Sky Pirates/Anim/Anim Set", fileName = "AnimSet")]
    public sealed class AnimSet : ScriptableObject
    {
        [SerializeField] private AnimProfile _structureProfile = null!;
        [SerializeField] private AnimClipDefinition[] _clips = Array.Empty<AnimClipDefinition>();

        internal AnimProfile StructureProfile => _structureProfile;
        internal AnimClipDefinition[] Clips => _clips;

        private void OnValidate()
        {
            AnimAssetChangeNotifier.NotifyChanged(this);
        }
    }
}
