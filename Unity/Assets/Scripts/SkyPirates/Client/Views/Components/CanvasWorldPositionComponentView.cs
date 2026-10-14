using Delta;
using Delta.ECS.Unity;
using SkyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace SkyPirates.Client.Views.Components
{
    public class CanvasWorldPositionComponentView : MonoBehaviour, IEntityView
    {
        [SerializeField]
        private float3 _worldOffset;

        private EntityContext _context;
        private float3 _position;
        private float3 _velocity;

        private static Camera _camera;
        private static Camera Camera => _camera == null ? _camera = Camera.main : _camera;

        public void OnBind(EntityContext context)
        {
            _context = context;
            _position = Position + _worldOffset;
            transform.position = Camera.WorldToScreenPoint(_position);
        }

        private void Update()
        {
            if (!EntityViewLifetime.TryEnterUpdate(this, _context))
                return;

            float3 targetPos = Position + _worldOffset;
            _position = float3.SmoothDamp(_position, targetPos, ref _velocity, LerpConstants.SmoothMoveTime, Time.deltaTime);
            transform.position = Camera.WorldToScreenPoint(_position);
        }

        private float3 Position => (float3)_context.Get<Position>().Value;
    }
}
