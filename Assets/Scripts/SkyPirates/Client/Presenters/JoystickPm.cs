using DVG.Core;
using DVG.SkyPirates.Client.IViews;

namespace DVG.SkyPirates.Client.Presenters
{
    public class JoystickPm : Presenter<IJoystickView, object>
    {
        public float2 Direction => View.Direction;
        public bool Fixation => View.Fixation;

        public JoystickPm(IJoystickView joystickView) : base(joystickView, null) { }
    }
}
