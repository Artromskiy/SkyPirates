using Delta;
using DVG;
using DVG.SkyPirates.Rendering.SphereOverlay.View;
using DVG.SkyPirates.Client.Views;
using DVG.SkyPirates.Client.Views.Components;
using DVG.SkyPirates.Shared.Components.Config;
using UnityEngine;

namespace SkyPirates.Client.Views.Components
{
    public class ImpactZoneComponentView : ComponentView
    {
        [SerializeField] private SphereOverlayView _sphereOverlayView;

        private float _radius;
        private float _radiusVel;

        public override void OnInject()
        {
            _radius = Radius;
            UpdateVolume();
        }

        public override void Tick()
        {
            _radius = Maths.SmoothDamp(_radius, Radius, ref _radiusVel, LerpConstants.SmoothMoveTime, Time.deltaTime);
            UpdateVolume();
        }

        public override void Dispose()
        {
            if (_sphereOverlayView != null)
                _sphereOverlayView.ClearVolume();
        }

        private void UpdateVolume()
        {
            float radius = Mathf.Max(0f, _radius);
            if (_sphereOverlayView != null)
                _sphereOverlayView.SetRadius(radius);
        }

        private float Radius => (float)ViewModel.Get<ImpactDistance>().Value;
    }
}
