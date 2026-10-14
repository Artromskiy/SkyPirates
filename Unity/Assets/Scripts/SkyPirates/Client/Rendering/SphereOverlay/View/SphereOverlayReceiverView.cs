using System.Collections.Generic;
using DVG.SkyPirates.Rendering.SphereOverlay.Internals;
using UnityEngine;

namespace DVG.SkyPirates.Rendering.SphereOverlay.View
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Sky Pirates/Rendering/Sphere Overlay Receiver")]
    public sealed class SphereOverlayReceiverView : MonoBehaviour
    {
        private readonly Dictionary<Renderer, uint> _originalMasks = new Dictionary<Renderer, uint>();
        private readonly List<Renderer> _children = new List<Renderer>();
        private readonly HashSet<Renderer> _currentChildren = new HashSet<Renderer>();
        private readonly List<Renderer> _removedChildren = new List<Renderer>();

        private void OnEnable() => RefreshRenderers();

        private void OnDisable() => RestoreMasks();

        private void OnTransformChildrenChanged()
        {
            if (isActiveAndEnabled)
                RefreshRenderers();
        }

        private void RefreshRenderers()
        {
            _children.Clear();
            GetComponentsInChildren(includeInactive: true, _children);
            _currentChildren.Clear();
            for (var i = 0; i < _children.Count; i++)
            {
                var renderer = _children[i];
                if (renderer != null)
                    _currentChildren.Add(renderer);
            }

            _removedChildren.Clear();
            foreach (var pair in _originalMasks)
            {
                if (pair.Key != null && _currentChildren.Contains(pair.Key))
                    continue;

                RestoreMask(pair.Key, pair.Value);
                _removedChildren.Add(pair.Key);
            }

            for (var i = 0; i < _removedChildren.Count; i++)
                _originalMasks.Remove(_removedChildren[i]);

            for (var i = 0; i < _children.Count; i++)
            {
                var renderer = _children[i];
                if (renderer == null)
                    continue;

                if (!_originalMasks.TryGetValue(renderer, out var originalMask))
                {
                    originalMask = renderer.renderingLayerMask;
                    _originalMasks.Add(renderer, originalMask);
                }

                renderer.renderingLayerMask =
                    originalMask | SphereOverlayRenderConstants.ReceiverRenderLayerMask;
            }
        }

        private void RestoreMasks()
        {
            foreach (var pair in _originalMasks)
                RestoreMask(pair.Key, pair.Value);

            _originalMasks.Clear();
        }

        private static void RestoreMask(Renderer renderer, uint originalMask)
        {
            if (renderer == null)
                return;

            var currentMask = renderer.renderingLayerMask;
            renderer.renderingLayerMask =
                (currentMask & ~SphereOverlayRenderConstants.ReceiverRenderLayerMask) |
                (originalMask & SphereOverlayRenderConstants.ReceiverRenderLayerMask);
        }
    }
}
