using Delta.Netcode;
using DVG.Components;
using DVG.SkyPirates.Shared.Data;

namespace DVG.SkyPirates.Shared.Commands
{
    [NetCommand(Id = 4, Predicted = true)]
    public struct SetEntityDataCommand
    {
        public SyncId Target;
        public ComponentDataSet Components;
    }
}
