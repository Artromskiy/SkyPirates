using DVG.Core;

namespace DVG.SkyPirates.Client.IViewModels
{
    public interface IJoystickVM : IViewModel
    {
        (float2 direction, bool fixation) Joystick { set; }
    }
}
