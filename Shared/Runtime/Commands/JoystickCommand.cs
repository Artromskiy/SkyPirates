using Delta;
using Delta.Netcode;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;

namespace SkyPirates.Shared.Commands
{
    [NetCommand(Id = 2, Predicted = true)]
    public struct JoystickCommand
    {
        public SyncId Target;
        public fix2 Direction;
        public bool Fixation;
    }
}
