using Delta.ECS;
using SkyPirates.Shared.Ids;
using System.Collections.Immutable;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("GoodsDrop")]
    [ComponentGroup(Group.History)]
    public readonly struct GoodsDrop
    {
        public readonly ImmutableSortedDictionary<GoodsId, int> Values { get; init; }
    }
}
