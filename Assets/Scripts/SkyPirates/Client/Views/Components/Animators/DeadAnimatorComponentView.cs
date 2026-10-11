using Delta;
using DVG.Components;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components.Animators
{
    public class DeadAnimatorComponentView : ComponentView
    {
        private float _layerWeight;
        private bool _animViewState;
        private bool _hasAnimViewState;

        private int DeadLayer => Animator.GetLayerIndex("DeadLayer");
        private static readonly int DeadStateKey = Animator.StringToHash("Dead");
        private Animator Animator => GetComponentInChildren<Animator>();
        private AnimView Anim => GetComponentInChildren<AnimView>();

        public override void OnInject()
        {

        }

        public override void Tick()
        {
            bool dead = !ViewModel.Has<Alive>();

            AnimView animView = Anim;
            if (animView != null)
            {
                if (!animView.IsReady)
                    return;

                if (!_hasAnimViewState || _animViewState != dead)
                {
                    _animViewState = dead;
                    _hasAnimViewState = true;
                    if (dead)
                        animView.Play("Life.Dead");
                    else
                        animView.StopLayer("DeadLayer");
                }

                return;
            }

            _layerWeight = Maths.MoveTowards(_layerWeight, dead ? 1 : 0, Time.deltaTime * Constants.TicksPerSecond);

            if (Animator == null)
                return;

            if (dead)
                Animator.Play(DeadStateKey, DeadLayer);

            Animator.SetLayerWeight(DeadLayer, _layerWeight);
        }
    }
}
