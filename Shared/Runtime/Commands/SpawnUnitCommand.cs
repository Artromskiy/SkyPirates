using Delta.Netcode;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ids;

namespace SlyPirates.Shared.Commands
{
    [CommandSchema(6, Predicted = false)]
    public struct SpawnUnitCommand
    {
        public SyncId SquadId;

        public UnitId UnitId;
        public EntityParameters CreationData;
    }
}
