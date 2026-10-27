using Delta;
using Delta.ECS;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0x5FA8F09FC492E523UL)]
    [ComponentAlias("SpawnPoint")]
    [ComponentGroup(Group.History)]
    public struct SpawnPoint
    {
        public int2 ShipCoordinates;
    }
}
