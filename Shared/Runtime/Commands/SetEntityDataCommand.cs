using Delta.Netcode;
using DVG.SkyPirates.Shared.Components;
using DVG.SkyPirates.Shared.Components.Runtime;
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
