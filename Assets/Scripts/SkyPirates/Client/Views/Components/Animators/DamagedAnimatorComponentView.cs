using DVG.SkyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components.Animators
{
    public class DamagedAnimatorComponentView : ComponentView
    {
        private Animator _animator;
        private float _prevHealth;

        private int DamageLayer => Animator.GetLayerIndex("DamageLayer");
        private static readonly int DamagedStateKey = Animator.StringToHash("Damage");
        private Animator Animator => _animator != null ? _animator : _animator = GetComponentInChildren<Animator>();

        public override void OnInject()
        {
            _prevHealth = (float)ViewModel.Get<Health>().Value;
        }

        public override void Tick()
        {
            var health = (float)ViewModel.Get<Health>().Value;
            bool healthChanged = health < _prevHealth;
            _prevHealth = health;
            if (healthChanged && Animator != null)
            {
                Animator.CrossFadeInFixedTime(
                    DamagedStateKey,
                    LerpConstants.AnimationTime,
                    DamageLayer);
            }
        }

    }
}
