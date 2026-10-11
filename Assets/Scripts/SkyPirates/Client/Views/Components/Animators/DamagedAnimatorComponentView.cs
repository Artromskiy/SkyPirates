using DVG.SkyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components.Animators
{
    public class DamagedAnimatorComponentView : ComponentView
    {
        private float _prevHealth;

        private int DamageLayer => Animator.GetLayerIndex("DamageLayer");
        private static readonly int DamagedStateKey = Animator.StringToHash("Damage");
        private Animator Animator => GetComponentInChildren<Animator>();
        private AnimView Anim => GetComponentInChildren<AnimView>();

        public override void OnBound()
        {
            _prevHealth = (float)Get<Health>().Value;
        }

        public override void Tick()
        {
            float health = (float)Get<Health>().Value;
            bool healthChanged = health < _prevHealth;
            _prevHealth = health;
            if (!healthChanged)
                return;

            AnimView animView = Anim;
            if (animView != null)
            {
                if (animView.IsReady)
                    animView.PlayOnce("Reaction.Damage");
                return;
            }

            if (Animator != null)
            {
                Animator.CrossFadeInFixedTime(
                    DamagedStateKey,
                    LerpConstants.AnimationTime,
                    DamageLayer);
            }
        }

    }
}
