using Delta;
using Delta.Netcode;
using SlyPirates.Shared.Components.Runtime;

namespace SlyPirates.Shared.Commands
{
    [CommandSchema(0xCF377F2A0578AECDUL, Predicted = true)]
    public struct JoystickCommand
    {
        public SyncId Target;
        public fix2 Direction;
        public bool Fixation;
    }
}
