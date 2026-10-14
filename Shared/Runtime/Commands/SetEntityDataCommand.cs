using Delta.Netcode;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Data;

namespace SkyPirates.Shared.Commands
{
    [NetCommand(Id = 4, Predicted = true)]
    public struct SetEntityDataCommand
    {
        public SyncId Target;
        public ComponentDataSet Components;
    }
}
