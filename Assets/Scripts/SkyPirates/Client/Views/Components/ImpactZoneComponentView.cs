using DVG;
using DVG.SkyPirates.Client.Views;
using DVG.SkyPirates.Client.Views.Components;
using DVG.SkyPirates.Shared.Components.Config;
using UnityEngine;

namespace SkyPirates.Client.Views.Components
{
    public class ImpactZoneComponentView : ComponentView
    {
        [SerializeField]
        private SpriteRenderer _renderer;

        private float _radius;
        private float _radiusVel;

        public override void OnInject()
        {
            _radius = Radius;
            _renderer.transform.localScale = new float3(_radius);
        }

        public override void Tick()
        {
            _radius = Maths.SmoothDamp(_radius, Radius, ref _radiusVel, LerpConstants.SmoothMoveTime, Time.deltaTime);
            _renderer.transform.localScale = new Vector3(_radius, _radius, _radius);
        }

        private float Radius => (float)ViewModel.Get<ImpactDistance>().Value;
    }
}
