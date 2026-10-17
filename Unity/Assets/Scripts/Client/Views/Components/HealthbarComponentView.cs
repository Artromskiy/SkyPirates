using Delta.ECS.Unity;
using DG.Tweening;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlyPirates.Client.Views.Components
{
    public class HealthbarComponentView : MonoBehaviour, IEntityView
    {
        private EntityContext _context;
        [SerializeField]
        private Image _fillImage = null!;
        [SerializeField]
        private CanvasGroup _canvasGroup = null!;
        [SerializeField]
        private TMP_Text _level = null!;
        [SerializeField]
        private int _debugRecolorId;

        private float _healthPercent;
        private int _displayedLevel = int.MinValue;
        private bool _hidden;

        private Tween _amountTween;
        private Tween _fadeTween;


        public void OnBind(EntityContext context)
        {
            _amountTween?.Kill();
            _fadeTween?.Kill();
            _context = context;
            float health = Health;
            float maxHealth = MaxHealth;
            _hidden = !_context.Has<Alive>() || _context.Has<Disabled>();
            _healthPercent = health / maxHealth;
            _canvasGroup.alpha = _hidden || _healthPercent == 1 ? 0 : 1;
            _fillImage.fillAmount = _healthPercent;

            UpdateLevel();
            Recolor(TeamId);
        }

        private void Update()
        {
            float health = Health;
            float maxHealth = MaxHealth;
            float percent = health / maxHealth;
            bool hidden = !_context.Has<Alive>() || _context.Has<Disabled>();

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
            {
                item.color = TeamIdToColor.RecolorHue(item.color, teamId);
            }
        }

        private float MaxHealth => (float)_context.Get<MaxHealth>().Value;
        private float Health => (float)_context.Get<Health>().Value;
        private int TeamId => _context.Get<TeamId>().Value;

        private void UpdateLevel()
        {
            int level = _context.Has<Level>() ? _context.Get<Level>().Value : 1;
            if (_displayedLevel == level)
            {
                return;
            }

            _displayedLevel = level;
            _level.text = level.ToString();
        }

        private void OnDestroy()
        {
            _amountTween?.Kill();
            _fadeTween?.Kill();
        }
    }
}
