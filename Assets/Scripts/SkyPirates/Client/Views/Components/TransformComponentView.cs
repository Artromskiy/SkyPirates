using Delta;
using DVG.SkyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class TransformComponentView : ComponentView
    {
        [SerializeField]
        private InterpolationMode _mode;
        [SerializeField]
        private float _sharpness;

        private float3 _velocity;
        private float _angularVelocity;
        [SerializeField]
        private float3 _realPosition;

        public override void OnBound()
        {
            Animate(InterpolationMode.Instant);
        }

        public override void Tick()
        {
            Animate(_mode);
        }

        private void Animate(InterpolationMode mode)
        {
            _realPosition = Position;

            if (mode == InterpolationMode.None)
                return;

            var tran = transform;
            float3 position = tran.position;
            float rotation = tran.rotation.eulerAngles.y;

            position = AnimatePosition(mode, position, Position);
            rotation = AnimateRotation(mode, rotation, Rotation);

            tran.SetPositionAndRotation(position, Quaternion.Euler(0, rotation, 0));
        }


        private float3 AnimatePosition(InterpolationMode mode, float3 from, float3 to)
        {
            return mode switch
            {
                InterpolationMode.SmoothDamp => float3.SmoothDamp(
                    from, to, ref _velocity,
                    1 / _sharpness,
                    Time.deltaTime),
                InterpolationMode.Lerp => float3.Lerp(
                    from, to, Maths.Clamp(1 - Maths.Exp(-_sharpness * Time.deltaTime), 0, 1)),
                _ => to,
            };
        }
        private float AnimateRotation(InterpolationMode mode, float from, float to)
        {
            return mode switch
            {
                InterpolationMode.SmoothDamp => Maths.SmoothDampAngle(
                    from, to, ref _angularVelocity,
                    1 / _sharpness,
                    Time.deltaTime),
                InterpolationMode.Lerp => Maths.Lerp(
                    from, from + Maths.DeltaAngle(from, to), 1 - Maths.Exp(-_sharpness * Time.deltaTime)),
                _ => to,
            };
        }


        private float3 Position
        {
            get => (float3)Get<Position>().Value;
            set => Set<Position>().Value = (fix3)value;
        }

        private float Rotation
        {
            get => (float)Get<Rotation>().Value;
            set => Set<Rotation>().Value = (fix)value;
        }

        private void Awake() => EditorChanges.SubscribeOnChanged(transform, UpdateTransform);
        protected override void OnDestroy()
        {
            EditorChanges.UnsubscribeOnChanged(transform, UpdateTransform);
            base.OnDestroy();
        }

        private void UpdateTransform()
        {
            if (!IsAlive)
                return;
            Position = gameObject.transform.position;
            Rotation = gameObject.transform.eulerAngles.y;
        }


        private enum InterpolationMode
        {
            None,
            Instant,
            Lerp,
            SmoothDamp,
        }
    }
}
