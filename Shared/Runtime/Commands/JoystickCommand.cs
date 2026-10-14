using Delta;
using Delta.Netcode;
using DVG.Components;

namespace DVG.SkyPirates.Shared.Commands
{
    [NetCommand(Id = 2, Predicted = true)]
    public struct JoystickCommand
    {
        public SyncId Target;
        public fix2 Direction;
        public bool Fixation;
    }
}
