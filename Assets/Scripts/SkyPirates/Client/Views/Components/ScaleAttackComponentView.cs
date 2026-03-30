using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Ids;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class ScaleAttackComponentView : ComponentView
    {
        [SerializeField]
        private Transform _scaleRoot;

        private float _statePercentVelocity;
        private float _statePercent;

        public override void OnInject() { }

        public override void Tick()
        {
            var moveTo = State == StateId.None ? 0 : StatePercent;
            _statePercent = Maths.SmoothDamp(_statePercent, moveTo, ref _statePercentVelocity, LerpConstants.AnimationTime, UnityEngine.Time.deltaTime);
            _scaleRoot.localScale = new float3(1f - 0.5f * _statePercent);
        }

        private StateId State => ViewModel.Get<BehaviourState>().State;
        private float StatePercent => (float)ViewModel.Get<BehaviourState>().Percent;
    }
}
