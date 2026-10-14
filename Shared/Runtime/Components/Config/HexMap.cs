using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Components;
using DVG.SkyPirates.Shared.Ids;
using System.Collections.Frozen;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("HexMap")]
    [ComponentGroup(Group.History)]
    public struct HexMap
    {
        public FrozenDictionary<int3, TileId> Data;
    }
}
