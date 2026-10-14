using Delta;
using SkyPirates.Shared.Core;
using SkyPirates.Shared;

namespace SkyPirates.Client.IViewModels
{
    public interface IJoystickVM : IViewModel
    {
        (float2 direction, bool fixation) Joystick { set; }
    }
}
