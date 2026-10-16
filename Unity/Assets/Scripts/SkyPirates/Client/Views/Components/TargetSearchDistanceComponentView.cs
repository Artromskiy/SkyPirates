using Delta;
using Delta.ECS.Unity;
using SlyPirates.Rendering.SphereOverlay.View;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace SlyPirates.Client.Views.Components
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
