using Delta;
using Delta.ECS.Unity;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Ids;
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

        public void OnBind(EntityContext context)
        {
            _context = context;
            _statePercentVelocity = 0;
            Animate(false);
        }

        public void OnVisualInstanceLoaded(GameObject visual)
        {
            _animView = visual.GetComponentInChildren<AnimView>(true);
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
                else
                {
                    _animView.Play(clipHolder, fadeSeconds);
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
