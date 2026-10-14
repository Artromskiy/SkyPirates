using Delta.Netcode;
using SkyPirates.Shared.Data;

namespace SkyPirates.Shared.Commands
{
    [NetCommand(Id = 5, Predicted = false)]
    public struct SpawnSquadCommand
    {
        public EntityParameters CreationData;
    }
}
