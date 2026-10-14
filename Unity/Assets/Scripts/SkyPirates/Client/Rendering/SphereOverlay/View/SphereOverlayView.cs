using UnityEngine;
using DVG.SkyPirates.Rendering.SphereOverlay.Internals;

namespace DVG.SkyPirates.Rendering.SphereOverlay.View
{
    [DisallowMultipleComponent]
    public sealed class SphereOverlayView : MonoBehaviour
    {
        [SerializeField] private Gradient _gradient = new Gradient();
        [SerializeField] private Color _color = Color.white;
        private float _radius;

        public Gradient Gradient
        {
            get => _gradient;
            set
            {
                _gradient = value;
                RefreshVolume();
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                RefreshVolume();
            }
        }

        public void SetRadius(float radius)
        {
            _radius = radius;
            RefreshVolume();
        }

        public void ClearVolume()
        {
            SphereOverlayRegistry.Remove(this);
        }

        private void OnDisable()
        {
            ClearVolume();
        }

        private void OnEnable() => RefreshVolume();

        private void OnValidate() => RefreshVolume();

        private void RefreshVolume()
        {
            if (isActiveAndEnabled && _radius > 0f)
                SphereOverlayRegistry.Set(this, transform.position, _radius, _gradient, _color);
        }
    }
}
