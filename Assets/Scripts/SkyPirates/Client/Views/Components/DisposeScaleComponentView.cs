using Delta;
using DVG.Components;
using DG.Tweening;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class DisposeScaleComponentView : ComponentView
    {
        [SerializeField]
        private float _delay = LerpConstants.AnimationTime;

        private bool _alive;
        private Tween _tween;

        private const float DisposeScale = 1.3f;
        private const float TweenDuration = 0.3f / 2;

        public override void OnBound()
        {
            _alive = Has<Alive>();
            transform.localScale = new float3(_alive ? 1 : 0);
        }

        public override void Tick()
        {
            bool alive = Has<Alive>();
            if (_alive == alive)
                return;

            _alive = alive;

            _tween?.Kill();
            _tween = DOTween.Sequence().
                AppendInterval(_delay).
                Append(transform.DOScale(DisposeScale, TweenDuration)).
                Append(transform.DOScale(_alive ? 1 : 0, TweenDuration));
        }
    }
}