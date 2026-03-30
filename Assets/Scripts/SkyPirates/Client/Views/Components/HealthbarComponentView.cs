using DG.Tweening;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class HealthbarComponentView : ComponentView
    {
        [SerializeField]
        private Image _fillImage = null!;
        [SerializeField]
        private CanvasGroup _canvasGroup = null!;
        [SerializeField]
        private float _verticalOffset;
        [SerializeField]
        private int _debugRecolorId;

        private float _healthPercent;
        private bool _hidden;

        private Tween _amountTween;
        private Tween _fadeTween;


        public override void OnInject()
        {
            var health = Health;
            var maxHealth = MaxHealth;
            _hidden = !ViewModel.Alive || ViewModel.Disabled;
            _healthPercent = health / maxHealth;
            _canvasGroup.alpha = _hidden || _healthPercent == 1 ? 0 : 1;
            _fillImage.fillAmount = _healthPercent;

            Recolor(TeamId);
        }

        public override void Tick()
        {
            var health = Health;
            var maxHealth = MaxHealth;
            float percent = health / maxHealth;
            var hidden = !ViewModel.Alive || ViewModel.Disabled;

            if (_hidden != hidden || _healthPercent != percent)
            {
                _fadeTween?.Kill();
                _hidden = hidden;
                var alpha = (_hidden || percent == 1) ? 0 : 1;
                _fadeTween = _canvasGroup.DOFade(alpha, LerpConstants.SmoothMoveTime);
            }

            if (_healthPercent != percent)
            {
                _amountTween?.Kill();
                _healthPercent = percent;
                _amountTween = _fillImage.DOFillAmount(_healthPercent, LerpConstants.SmoothMoveTime);
            }
        }

        [Button]
        private void DebugRecolor()
        {
            Recolor(_debugRecolorId);
        }

        private void Recolor(int teamId)
        {
            foreach (var item in GetComponentsInChildren<Image>(true))
                item.color = TeamIdToColor.RecolorHue(item.color, teamId);
        }

        private float MaxHealth => (float)ViewModel.Get<MaxHealth>().Value;
        private float Health => (float)ViewModel.Get<Health>().Value;
        private int TeamId => ViewModel.Get<TeamId>().Value;
    }
}
