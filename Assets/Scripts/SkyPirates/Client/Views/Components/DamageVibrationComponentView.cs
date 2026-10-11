using DVG.SkyPirates.Shared.Components.Runtime;

namespace DVG.SkyPirates.Client.Views.Components
{
    internal class DamageVibrationComponentView : ComponentView
    {
        private float _health;
        private bool _vibrate;

        public override void OnBound()
        {
            _health = Health;
            _vibrate = Get<TeamId>().Value == 0;
        }

        public override void Tick()
        {
            if (!_vibrate)
                return;

            if (Health >= _health)
            {
                _health = Health;
                return;
            }

            _health = Health;

            Vibration.Init();
            Vibration.VibratePop();
        }

        private float Health => (float)Get<Health>().Value;
    }
}