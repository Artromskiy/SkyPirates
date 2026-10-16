using Delta.Netcode;
using SlyPirates.Shared.Data;

namespace SlyPirates.Shared.Commands
{
    [NetCommand(Id = 3, Predicted = false)]
    public struct LoadWorldCommand
    {
        public WorldData WorldData;
    }
}
