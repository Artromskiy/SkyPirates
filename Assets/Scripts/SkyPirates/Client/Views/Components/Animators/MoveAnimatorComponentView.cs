using Delta;
using DVG.SkyPirates.Shared.Components.Config;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components.Animators
{
    public class MoveAnimatorComponentView : ComponentView
    {
        private float3 _prevPosition;
        private bool _isIdle;

        private int MoveLayer => Animator.GetLayerIndex("MoveLayer");
        private static readonly int MoveXKey = Animator.StringToHash("SpeedX");
        private static readonly int MoveZKey = Animator.StringToHash("SpeedZ");
        private static readonly int MoveStateKey = Animator.StringToHash("Move");
        private static readonly int IdleStateKey = Animator.StringToHash("Idle");
        private Animator Animator => GetComponentInChildren<Animator>();
        private AnimView Anim => GetComponentInChildren<AnimView>();

        public override void OnInject()
        {
            _prevPosition = transform.position;
        }

        public override void Tick()
        {
            float3 currentPosition = transform.position;
            var worldVelocity = (currentPosition - _prevPosition) / Time.deltaTime;
            _prevPosition = currentPosition;
            float maxSpeed = (float)ViewModel.Get<MaxSpeed>().Value;
            var localVelocity = transform.InverseTransformVector(worldVelocity) / maxSpeed;
            bool isIdle = localVelocity.sqrMagnitude < 0.01f;
            bool isIdleChanged = _isIdle != isIdle;
            _isIdle = isIdle;

            AnimView animView = Anim;
            if (animView != null)
            {
                if (animView.IsReady)
                    animView.Blend2D("Locomotion", new Vector2(localVelocity.x, localVelocity.z), 0f);
                return;
            }

            if (Animator == null)
                return;

            Animator.SetFloat(MoveXKey, localVelocity.x);
            Animator.SetFloat(MoveZKey, localVelocity.z);

            if (isIdleChanged)
            {
                int state = isIdle ? IdleStateKey : MoveStateKey;
                Animator.CrossFadeInFixedTime(state, LerpConstants.AnimationTime, MoveLayer);
            }
        }

    }
}
