using Delta;
using DVG.SkyPirates.Shared;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Ids;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components.Animators
{
    public class BehaviourAnimatorComponentView : ComponentView
    {
        private const string BehaviourLayerName = "BehaviourLayer";

        [SerializeField]
        private bool _animate;


        private float _layerWeight;

        private StateId _state;
        private float _statePercent;
        private float _statePercentVelocity;

        private StateId _prevState;
        private float _prevStatePercent;
        private float _transition;

        private StateId _animViewState;
        private bool _hasAnimViewState;

        private int BehaviourLayer => Animator.GetLayerIndex(BehaviourLayerName);
        private Animator Animator => GetComponentInChildren<Animator>();
        private AnimView Anim => GetComponentInChildren<AnimView>();

        public override void OnInject()
        {
            Animate(false);
        }

        public override void Tick()
        {
            Animate(_animate);
        }

        private void Animate(bool animate)
        {
            if (AnimateWithAnimView(animate))
                return;

            AnimateWithAnimator(animate);
        }

        private void AnimateWithAnimator(bool animate)
        {
            var newState = State;
            if (_state != newState)
            {
                if (_transition == 1)
                {
                    _prevState = _state;
                    _prevStatePercent = _statePercent;
                    _transition = 0;
                }
                _state = newState;
                _statePercent = 0;
                _statePercentVelocity = 0;
            }

            bool idle = _state == StateId.None;
            int layerWeight = idle ? 0 : 1;

            float crossFadeDuration = LerpConstants.AnimationTime * 2;

            _transition = !animate ?
                1 :
                Maths.MoveTowards(_transition, 1,
                Time.deltaTime / crossFadeDuration);

            _statePercent = !animate ?
                StatePercent :
                Maths.SmoothDamp(
                _statePercent,
                StatePercent,
                ref _statePercentVelocity,
                LerpConstants.AnimationTime,
                Time.deltaTime);

            _layerWeight = !animate ?
                layerWeight :
                Maths.MoveTowards(
                _layerWeight, layerWeight,
                Constants.TicksPerSecond * Time.deltaTime);

            if (Animator == null)
                return;

            Animator.SetLayerWeight(BehaviourLayer, _layerWeight);
            if (_transition != 1)
            {
                Animator.Play(_prevState, BehaviourLayer, _prevStatePercent);
                Animator.Update(0); // Do not delete, unity is too dump and crossfade not working properly
                var info = Animator.GetCurrentAnimatorStateInfo(BehaviourLayer);
                float normalizedDuration = crossFadeDuration / info.length;
                Animator.CrossFade(_state, normalizedDuration, BehaviourLayer, _statePercent, _transition);
            }
            else
            {
                Animator.Play(_state, BehaviourLayer, _statePercent);
            }
        }

        private StateId State => ViewModel.Get<BehaviourState>().State;
        private float StatePercent => (float)ViewModel.Get<BehaviourState>().Percent;

        private bool AnimateWithAnimView(bool animate)
        {
            AnimView animView = Anim;
            if (animView == null)
                return false;
            if (!animView.IsReady)
                return true;

            StateId state = State;
            string clipHolder = GetClipHolder(state);
            bool stateChanged = !_hasAnimViewState || _animViewState != state;
            if (stateChanged)
            {
                float fadeSeconds = _hasAnimViewState && animate
                    ? AnimView.DefaultFadeSeconds
                    : 0f;

                _animViewState = state;
                _hasAnimViewState = true;

                if (string.IsNullOrEmpty(clipHolder))
                    animView.StopLayer(BehaviourLayerName, fadeSeconds);
                else
                {
                    animView.Play(clipHolder, fadeSeconds);
                    animView.SetClipHolderTime(clipHolder, StatePercent);
                }
            }

            return true;
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
