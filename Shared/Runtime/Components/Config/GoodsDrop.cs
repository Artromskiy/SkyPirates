using Delta.ECS;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Ids;
using System.Collections.Immutable;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("GoodsDrop")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public readonly struct GoodsDrop
    {
        public readonly ImmutableSortedDictionary<GoodsId, int> Values { get; init; }
    }
}
