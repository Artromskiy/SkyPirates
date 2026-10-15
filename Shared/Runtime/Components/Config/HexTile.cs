using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("HexTile")]
    [ComponentGroup(Group.History)]
    public struct HexTile
    {
        public int2 Coordinates;
        public int Elevation;
    }
}
