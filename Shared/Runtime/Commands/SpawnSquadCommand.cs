using Delta.Netcode;
using DVG.SkyPirates.Shared.Data;

namespace DVG.SkyPirates.Shared.Commands
{
    [NetCommand(Id = 5, Predicted = false)]
    public struct SpawnSquadCommand
    {
        public EntityParameters CreationData;
    }
}
