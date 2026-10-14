using Delta;
using Delta.ECS.Unity;
using DG.Tweening;
using SkyPirates.Shared.Components.Config;
using SkyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace SkyPirates.Client.Views.Components
{
    public class HealthScaleComponentView : MonoBehaviour, IEntityView
    {
        private EntityContext _context;
        [SerializeField]
        private Transform _scaleRoot;

        private const float HitScale = 1.25f;
        private const float MinScale = 0.7f;
        private const float TweenDuration = 0.25f / 2;

        private float _health;
        private Tween _tween;

        public void OnBind(EntityContext context) => _context = context;

        private void Update()
        {
            if (!EntityViewLifetime.TryEnterUpdate(this, _context))
                return;

            if (Health == _health)
                return;

            bool damaged = Health < _health;
            _health = Health;
            _tween?.Kill();
            float scaleTo = Maths.Max(MinScale, HealthPercent);
            _tween = damaged ?
                DOTween.Sequence().
                Append(_scaleRoot.DOScale(scaleTo * HitScale, TweenDuration)).
                Append(_scaleRoot.DOScale(scaleTo, TweenDuration)) :

                 _scaleRoot.DOScale(scaleTo, TweenDuration);
        }

        private float Health => (float)_context.Get<Health>().Value;
        private float HealthPercent => (float)(_context.Get<Health>().Value / _context.Get<MaxHealth>().Value);

        private void OnDestroy() => _tween?.Kill();
    }
}
