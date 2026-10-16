using Delta.Netcode;

namespace SlyPirates.Shared.Commands
{
    [NetCommand(Id = 1, Predicted = false)]
    public struct InvalidateCommand
    {
        public int CommandId;
    }
}
