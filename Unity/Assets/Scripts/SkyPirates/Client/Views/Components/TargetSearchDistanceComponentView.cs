using Delta;
using Delta.ECS.Unity;
using SkyPirates.Rendering.SphereOverlay.View;
using SkyPirates.Shared.Components.Config;
using SkyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace SkyPirates.Client.Views.Components
{
    public class TargetSearchDistanceComponentView : MonoBehaviour, IEntityView
    {
        [SerializeField]
        private SphereOverlayView _sphereOverlayView;

        private EntityContext _context;
        private float _radius;
        private float _radiusVel;

        public void OnBind(EntityContext context)
        {
            _context = context;
            _radius = Radius;
            if (_sphereOverlayView != null)
                _sphereOverlayView.Color = TeamIdToColor.RecolorHue(_sphereOverlayView.Color, _context.Get<TeamId>());
            UpdateVolume();
        }

        private void Update()
        {
            if (!EntityViewLifetime.TryEnterUpdate(this, _context))
                return;

            _radius = Maths.SmoothDamp(_radius, Radius, ref _radiusVel, LerpConstants.SmoothMoveTime, Time.deltaTime);
            UpdateVolume();
        }

        private void OnDestroy()
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

        private float Radius => (float)_context.Get<TargetSearchDistance>().Value;
    }
}
