using Delta;
using DVG.SkyPirates.Shared.Core;
using DVG.SkyPirates.Shared;

namespace DVG.SkyPirates.Client.IViewModels
{
    public interface IJoystickVM : IViewModel
    {
        (float2 direction, bool fixation) Joystick { set; }
    }
}
