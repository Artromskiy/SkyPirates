using Delta.ECS;
using SkyPirates.Shared.Core.Ids.Attributes;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Ids
{
    [Id(true)]
    [ComponentSchema]
    [ComponentAlias("GoodsId")]
    [ComponentGroup(Group.History)]
    public partial struct GoodsId { }
}
