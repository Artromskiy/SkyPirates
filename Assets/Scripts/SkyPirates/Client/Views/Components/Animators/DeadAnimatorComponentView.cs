using DVG.Components;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components.Animators
{
    public class DeadAnimatorComponentView : ComponentView
    {
        private Animator _animator;
        private float _layerWeight;

        private int DeadLayer => Animator.GetLayerIndex("DeadLayer");
        private static readonly int DeadStateKey = Animator.StringToHash("Dead");
        private Animator Animator => _animator != null ? _animator : _animator = GetComponentInChildren<Animator>();

        public override void OnInject()
        {

        }

        public override void Tick()
        {
            bool dead = !ViewModel.Has<Alive>();
            _layerWeight = Maths.MoveTowards(_layerWeight, dead ? 1 : 0, Time.deltaTime * Constants.TicksPerSecond);

            if (Animator == null)
                return;

            if (dead)
                Animator.Play(DeadStateKey, DeadLayer);

            Animator.SetLayerWeight(DeadLayer, _layerWeight);
        }
    }
}