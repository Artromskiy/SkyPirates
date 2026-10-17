using Delta.Netcode;

namespace SlyPirates.Shared.Commands
{
    [CommandSchema(0x515B16FA173138D0UL, Predicted = false)]
    public struct InvalidateCommand
    {
        public int CommandId;
    }
}
