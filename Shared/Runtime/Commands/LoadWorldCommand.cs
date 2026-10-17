using Delta.Netcode;
using SlyPirates.Shared.Data;

namespace SlyPirates.Shared.Commands
{
    [CommandSchema(0x926206D193958937UL, Predicted = false)]
    public struct LoadWorldCommand
    {
        public WorldData WorldData;
    }
}
