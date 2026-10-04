using UnityEngine;
using DVG.SkyPirates.Rendering.SphereOverlay.Internals;

namespace DVG.SkyPirates.Rendering.SphereOverlay.View
{
    [DisallowMultipleComponent]
    public sealed class SphereOverlayView : MonoBehaviour
    {
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private Color _centerColor = Color.white;
        [SerializeField] private Color _edgeColor = Color.white;

        public Color Color
        {
            get => _color;
            set => _color = value;
        }

        public Color CenterColor
        {
            get => _centerColor;
            set => _centerColor = value;
        }

        public Color EdgeColor
        {
            get => _edgeColor;
            set => _edgeColor = value;
        }

        public void SetRadius(float radius)
        {
            SphereOverlayRegistry.Set(this, transform.position, radius, _color, _centerColor, _edgeColor);
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
