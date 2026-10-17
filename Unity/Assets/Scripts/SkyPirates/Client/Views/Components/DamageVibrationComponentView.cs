using Delta.ECS.Unity;
using SlyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace SlyPirates.Client.Views.Components
{
    internal class DamageVibrationComponentView : MonoBehaviour, IEntityView
    {
        private EntityContext _context;
        private float _health;
        private bool _vibrate;

        public void OnBind(EntityContext context)
        {
            _context = context;
            _health = Health;
            _vibrate = _context.Get<TeamId>().Value == 0;
        }

        private void Update()
        {
            if (!_vibrate)
            {
                return;
            }

            if (Health >= _health)
            {
                _health = Health;
                return;
            }

            _health = Health;

            Vibration.Init();
            Vibration.VibratePop();
        }

        private float Health => (float)_context.Get<Health>().Value;
    }
}
