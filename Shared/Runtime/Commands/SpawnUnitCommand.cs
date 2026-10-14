using Delta.Netcode;
using DVG.Components;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Ids;

namespace DVG.SkyPirates.Shared.Commands
{
    [NetCommand(Id = 6, Predicted = false)]
    public struct SpawnUnitCommand
    {
        public SyncId SquadId;

        public UnitId UnitId;
        public EntityParameters CreationData;
    }
}
