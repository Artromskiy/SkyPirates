using DVG.SkyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class CanvasWorldPositionComponentView : ComponentView
    {
        [SerializeField]
        private float3 _worldOffset;

        private float3 _position;

        private float3 _velocity;

        private static Camera _camera;
        private static Camera Camera => _camera == null ? _camera = Camera.main : _camera;

        public override void OnInject()
        {
            _position = Position + _worldOffset;
            transform.position = Camera.WorldToScreenPoint(_position);
        }

        public override void Tick()
        {
            var targetPos = Position + _worldOffset;
            _position = float3.SmoothDamp(_position, targetPos, ref _velocity, LerpConstants.SmoothMoveTime, Time.deltaTime);
            transform.position = Camera.WorldToScreenPoint(_position);
        }

        private float3 Position => (float3)ViewModel.Get<Position>().Value;
    }
}
