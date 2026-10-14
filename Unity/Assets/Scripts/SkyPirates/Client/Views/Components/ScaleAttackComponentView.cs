using Delta;
using Delta.ECS.Unity;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Ids;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class ScaleAttackComponentView : MonoBehaviour, IEntityView
    {
        [SerializeField]
        private Transform _scaleRoot;

        private EntityContext _context;
        private float _statePercentVelocity;
        private float _statePercent;

        public void OnBind(EntityContext context) => _context = context;

        private void Update()
        {
            if (!EntityViewLifetime.TryEnterUpdate(this, _context))
                return;

            float moveTo = State == StateId.None ? 0 : StatePercent;
            _statePercent = Maths.SmoothDamp(_statePercent, moveTo, ref _statePercentVelocity, LerpConstants.AnimationTime, Time.deltaTime);
            _scaleRoot.localScale = new float3(1f - 0.5f * _statePercent);
        }

        private StateId State => _context.Get<BehaviourState>().State;
        private float StatePercent => (float)_context.Get<BehaviourState>().Percent;
    }
}
