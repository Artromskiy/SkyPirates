using Delta;
using Delta.ECS.Unity;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using DG.Tweening;
using UnityEngine;

namespace SkyPirates.Client.Views.Components
{
    public class DisposeScaleComponentView : MonoBehaviour, IEntityView
    {
        private EntityContext _context;
        [SerializeField]
        private float _delay = LerpConstants.AnimationTime;

        private bool _alive;
        private Tween _tween;

        private const float DisposeScale = 1.3f;
        private const float TweenDuration = 0.3f / 2;

        public void OnBind(EntityContext context)
        {
            _context = context;
            _alive = _context.Has<Alive>();
            transform.localScale = new float3(_alive ? 1 : 0);
        }

        private void Update()
        {
            if (!EntityViewLifetime.TryEnterUpdate(this, _context))
                return;

            bool alive = _context.Has<Alive>();
            if (_alive == alive)
                return;

            _alive = alive;

            _tween?.Kill();
            _tween = DOTween.Sequence().
                AppendInterval(_delay).
                Append(transform.DOScale(DisposeScale, TweenDuration)).
                Append(transform.DOScale(_alive ? 1 : 0, TweenDuration));
        }

        private void OnDestroy() => _tween?.Kill();
    }
}
