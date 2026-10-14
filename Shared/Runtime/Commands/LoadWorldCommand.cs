using Delta.Netcode;
using DVG.SkyPirates.Shared.Data;

namespace DVG.SkyPirates.Shared.Commands
{
    [NetCommand(Id = 3, Predicted = false)]
    public struct LoadWorldCommand
    {
        public WorldData WorldData;
    }
}
