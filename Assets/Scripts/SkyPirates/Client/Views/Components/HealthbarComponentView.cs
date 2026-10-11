using DG.Tweening;
using DVG.Components;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using TMPro;
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
        private TMP_Text _level = null!;
        [SerializeField]
        private float _verticalOffset;
        [SerializeField]
        private int _debugRecolorId;

        private float _healthPercent;
        private int _displayedLevel = int.MinValue;
        private bool _hidden;

        private Tween _amountTween;
        private Tween _fadeTween;


        public override void OnBound()
        {
            float health = Health;
            float maxHealth = MaxHealth;
            _hidden = !Has<Alive>() || Has<Disabled>();
            _healthPercent = health / maxHealth;
            _canvasGroup.alpha = _hidden || _healthPercent == 1 ? 0 : 1;
            _fillImage.fillAmount = _healthPercent;

            UpdateLevel();
            Recolor(TeamId);
        }

        public override void Tick()
        {
            float health = Health;
            float maxHealth = MaxHealth;
            float percent = health / maxHealth;
            bool hidden = !Has<Alive>() || Has<Disabled>();

            UpdateLevel();

            if (_hidden != hidden || _healthPercent != percent)
            {
                _fadeTween?.Kill();
                _hidden = hidden;
                int alpha = (_hidden || percent == 1) ? 0 : 1;
                _fadeTween = _canvasGroup.DOFade(alpha, LerpConstants.SmoothMoveTime);
            }

            if (_healthPercent != percent)
            {
                _amountTween?.Kill();
                _healthPercent = percent;
                _amountTween = _fillImage.DOFillAmount(_healthPercent, LerpConstants.SmoothMoveTime);
            }
        }

        [ContextMenu("Debug/Recolor")]
        private void DebugRecolor()
        {
            Recolor(_debugRecolorId);
        }

        private void Recolor(int teamId)
        {
            foreach (var item in GetComponentsInChildren<Image>(true))
                item.color = TeamIdToColor.RecolorHue(item.color, teamId);
        }

        private float MaxHealth => (float)Get<MaxHealth>().Value;
        private float Health => (float)Get<Health>().Value;
        private int TeamId => Get<TeamId>().Value;

        private void UpdateLevel()
        {
            int level = Has<Level>() ? Get<Level>().Value : 1;
            if (_displayedLevel == level)
                return;

            _displayedLevel = level;
            _level.text = level.ToString();
        }
    }
}
