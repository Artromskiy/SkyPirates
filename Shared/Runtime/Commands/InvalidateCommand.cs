using Delta.Netcode;

namespace SkyPirates.Shared.Commands
{
    [NetCommand(Id = 1, Predicted = false)]
    public struct InvalidateCommand
    {
        public int CommandId;
    }
}
