using UnityEngine;
using DVG.SkyPirates.Rendering.SphereOverlay.Internals;

namespace DVG.SkyPirates.Rendering.SphereOverlay.View
{
    [DisallowMultipleComponent]
    public sealed class SphereOverlayView : MonoBehaviour
    {
        [SerializeField] private Color _color = Color.white;

        public Color Color
        {
            get => _color;
            set => _color = value;
        }

        public void SetRadius(float radius)
        {
            SphereOverlayRegistry.Set(this, transform.position, radius, _color);
        }

        public void ClearVolume()
        {
            SphereOverlayRegistry.Remove(this);
        }

        private void OnDisable()
        {
            ClearVolume();
        }
    }
}
