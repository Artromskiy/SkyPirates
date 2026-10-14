using Delta;
using Delta.ECS;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Ids;
using System.Collections.Frozen;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("HexMap")]
    [ComponentGroup(Group.History)]
    public struct HexMap
    {
        public FrozenDictionary<int3, TileId> Data;
    }
}
