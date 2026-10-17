using Delta;
using Delta.ECS.Unity;
using SlyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace SlyPirates.Client.Views.Components
{
    public class TransformComponentView : MonoBehaviour, IEntityView
    {
        [SerializeField]
        private InterpolationMode _mode;
        [SerializeField]
        private float _sharpness;

        private EntityContext _context;
        private float3 _velocity;
        private float _angularVelocity;
        [SerializeField]
        private float3 _realPosition;

        public void OnBind(EntityContext context)
        {
            _context = context;
            _velocity = default;
            _angularVelocity = 0;
            Animate(InterpolationMode.Instant);
        }

        private void Update()
        {
            Animate(_mode);
        }

        private void Animate(InterpolationMode mode)
        {
            _realPosition = Position;
            if (mode == InterpolationMode.None)
            {
                return;
            }

            var tran = transform;
            float3 position = tran.position;
            float rotation = tran.rotation.eulerAngles.y;
            position = AnimatePosition(mode, position, Position);
            rotation = AnimateRotation(mode, rotation, Rotation);
            tran.SetPositionAndRotation(position, Quaternion.Euler(0, rotation, 0));
        }

        private float3 AnimatePosition(InterpolationMode mode, float3 from, float3 to) => mode switch
        {
            InterpolationMode.SmoothDamp => float3.SmoothDamp(from, to, ref _velocity, 1 / _sharpness, Time.deltaTime),
            InterpolationMode.Lerp => float3.Lerp(from, to, Maths.Clamp(1 - Maths.Exp(-_sharpness * Time.deltaTime), 0, 1)),
            _ => to,
        };

        private float AnimateRotation(InterpolationMode mode, float from, float to) => mode switch
        {
            InterpolationMode.SmoothDamp => Maths.SmoothDampAngle(from, to, ref _angularVelocity, 1 / _sharpness, Time.deltaTime),
            InterpolationMode.Lerp => Maths.Lerp(from, from + Maths.DeltaAngle(from, to), 1 - Maths.Exp(-_sharpness * Time.deltaTime)),
            _ => to,
        };

        private float3 Position => (float3)_context.Get<Position>().Value;

        private float Rotation => (float)_context.Get<Rotation>().Value;

        private enum InterpolationMode
        {
            None,
            Instant,
            Lerp,
            SmoothDamp,
        }
    }
}
