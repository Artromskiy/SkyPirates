using Delta.ECS;
using DVG.SkyPirates.Shared.Core.Ids.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Ids
{
    [Id(true)]
    [ComponentSchema]
    [ComponentAlias("GoodsId")]
    [ComponentGroup(Group.History)]
    public partial struct GoodsId { }
}
