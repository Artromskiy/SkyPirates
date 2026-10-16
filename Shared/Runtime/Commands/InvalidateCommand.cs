using Delta.Netcode;

namespace SlyPirates.Shared.Commands
{
    [CommandSchema(1, Predicted = false)]
    public struct InvalidateCommand
    {
        public int CommandId;
    }
}
