using Delta.Netcode;
using SlyPirates.Shared.Data;

namespace SlyPirates.Shared.Commands
{
    [CommandSchema(5, Predicted = false)]
    public struct SpawnSquadCommand
    {
        public EntityParameters CreationData;
    }
}
