using Delta.Netcode;
using SlyPirates.Shared.Data;

namespace SlyPirates.Shared.Commands
{
    [NetCommand(Id = 5, Predicted = false)]
    public struct SpawnSquadCommand
    {
        public EntityParameters CreationData;
    }
}
