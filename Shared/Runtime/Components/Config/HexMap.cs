using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Ids;
using System.Collections.Frozen;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("HexMap")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct HexMap
    {
        public FrozenDictionary<int3, TileId> Data;
    }
}
