using DG.Tweening;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class HealthScaleComponentView : ComponentView
    {
        [SerializeField]
        private Transform _scaleRoot;

        private const float HitScale = 1.25f;
        private const float MinScale = 0.7f;
        private const float TweenDuration = 0.25f / 2;

        private float _health;
        private Tween _tween;

        public override void OnInject() { }

        public override void Tick()
        {
            if (Health == _health)
                return;

            bool damaged = Health < _health;
            _health = Health;
            _tween?.Kill();
            var scaleTo = Maths.Max(MinScale, HealthPercent);
            _tween = damaged ?
                DOTween.Sequence().
                Append(_scaleRoot.DOScale(scaleTo * HitScale, TweenDuration)).
                Append(_scaleRoot.DOScale(scaleTo, TweenDuration)) :

                 _scaleRoot.DOScale(scaleTo, TweenDuration);
        }

        private float Health => (float)ViewModel.Get<Health>().Value;
        private float HealthPercent => (float)(ViewModel.Get<Health>().Value / ViewModel.Get<MaxHealth>().Value);
    }
}
