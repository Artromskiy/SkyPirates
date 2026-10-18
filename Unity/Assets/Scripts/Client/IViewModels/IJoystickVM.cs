using Delta;
using SlyPirates.Shared.Core;

namespace SlyPirates.Client.IViewModels
{
    public interface IJoystickVM : IViewModel
    {
        (float2 direction, bool fixation) Joystick { set; }
    }
}
