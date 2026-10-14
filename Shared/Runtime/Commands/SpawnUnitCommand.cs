using Delta.Netcode;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.Ids;

namespace SkyPirates.Shared.Commands
{
    [NetCommand(Id = 6, Predicted = false)]
    public struct SpawnUnitCommand
    {
        public SyncId SquadId;

        public UnitId UnitId;
        public EntityParameters CreationData;
    }
}
