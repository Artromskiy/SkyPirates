using Delta;
using Delta.ECS.Unity;
using DVG.SkyPirates.Shared;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Ids;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components.Animators
{
    public class BehaviourAnimatorComponentView : MonoBehaviour, IEntityView
    {
        private const string BehaviourLayerName = "BehaviourLayer";

        [SerializeField]
        private bool _animate;

        private float _statePercent;
        private float _statePercentVelocity;
        private StateId _animViewState;
        private bool _hasAnimViewState;
        private EntityContext _context;
        private AnimView _animView;

        private void Awake() => CacheComponents();

        public void OnBind(EntityContext context)
        {
            _context = context;
            CacheComponents();
            Animate(false);
        }

        private void Update()
        {
            if (EntityViewLifetime.TryEnterUpdate(this, _context))
                Animate(_animate);
        }

        private void Animate(bool animate)
        {
            if (_animView == null || !_animView.IsReady)
                return;

            BehaviourState behaviour = _context.Get<BehaviourState>();
            StateId state = behaviour.State;
            float targetPercent = (float)behaviour.Percent;
            string clipHolder = GetClipHolder(state);
            bool stateChanged = !_hasAnimViewState || _animViewState != state;
            if (stateChanged)
            {
                float fadeSeconds = _hasAnimViewState && animate
                    ? AnimView.DefaultFadeSeconds
                    : 0f;

                _animViewState = state;
                _hasAnimViewState = true;
                _statePercent = 0f;
                _statePercentVelocity = 0f;

                if (string.IsNullOrEmpty(clipHolder))
                    _animView.StopLayer(BehaviourLayerName, fadeSeconds);
                else if (!_animView.Play(clipHolder, fadeSeconds))
                    return;
            }

            if (string.IsNullOrEmpty(clipHolder))
                return;

            _statePercent = animate
                ? Maths.SmoothDamp(
                    _statePercent,
                    targetPercent,
                    ref _statePercentVelocity,
                    LerpConstants.AnimationTime,
                    Time.deltaTime)
                : targetPercent;
            _animView.SetClipHolderTime(clipHolder, _statePercent);
        }

        private void CacheComponents()
        {
            if (_animView == null)
                _animView = GetComponentInChildren<AnimView>();
        }

        private static string GetClipHolder(StateId state)
        {
            if (state == StateId.Constants.PreAttack)
                return "Behaviour.PreAttack";

            if (state == StateId.Constants.PostAttack)
                return "Behaviour.PostAttack";

            return string.Empty;
        }
    }
}
