using Delta.ECS;
using DVG.SkyPirates.Shared.Ids;
using System.Collections.Immutable;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("GoodsDrop")]
    [ComponentGroup(Group.History)]
    public readonly struct GoodsDrop
    {
        public readonly ImmutableSortedDictionary<GoodsId, int> Values { get; init; }
    }
}
