using Delta.Netcode;
using SkyPirates.Shared.Data;

namespace SkyPirates.Shared.Commands
{
    [NetCommand(Id = 3, Predicted = false)]
    public struct LoadWorldCommand
    {
        public WorldData WorldData;
    }
}
