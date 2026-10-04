using Delta;
using DVG.SkyPirates.Rendering.SphereOverlay.View;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class TargetSearchDistanceComponentView : ComponentView
    {
        [SerializeField]
        private SphereOverlayView _sphereOverlayView;

        private float _radius;
        private float _radiusVel;

        public override void OnInject()
        {
            _radius = Radius;
            if (_sphereOverlayView != null)
                _sphereOverlayView.Color = TeamIdToColor.RecolorHue(_sphereOverlayView.Color, ViewModel.Get<TeamId>());
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
            var radius = Mathf.Max(0f, _radius);
            if (_sphereOverlayView != null)
                _sphereOverlayView.SetRadius(radius);
        }

        private float Radius => (float)ViewModel.Get<TargetSearchDistance>().Value;
    }
}
