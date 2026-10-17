using Delta;
using Delta.ECS.Unity;
using SlyPirates.Shared;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Ids;
using SlyPirates.Client.Views.Components;
using UnityEngine;

namespace SlyPirates.Client.Views.Components.Animators
{
    public class BehaviourAnimatorComponentView : MonoBehaviour, IEntityView, IVisualInstanceLoaded
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
            _statePercentVelocity = 0;
            Animate(false);
        }

        public void OnVisualInstanceLoaded()
        {
            CacheComponents();
            _hasAnimViewState = false;
            _statePercentVelocity = 0;
            Animate(false);
        }

        private void Update()
        {
            Animate(_animate);
        }

        private void Animate(bool animate)
        {
            if (_animView == null || !_animView.IsReady)
            {
                return;
            }

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
                {
                    _animView.StopLayer(BehaviourLayerName, fadeSeconds);
                }
                else if (!_animView.Play(clipHolder, fadeSeconds))
                {
                    return;
                }
            }

            if (string.IsNullOrEmpty(clipHolder))
            {
                return;
            }

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
            {
                _animView = GetComponentInChildren<AnimView>();
            }
        }

        private static string GetClipHolder(StateId state)
        {
            if (state == StateId.Constants.PreAttack)
            {
                return "Behaviour.PreAttack";
            }

            if (state == StateId.Constants.PostAttack)
            {
                return "Behaviour.PostAttack";
            }

            return string.Empty;
        }
    }
}
