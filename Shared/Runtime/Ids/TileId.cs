using Delta.ECS;
using SkyPirates.Shared.Core.Ids.Attributes;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Ids
{
    [Id(false)]
    [ComponentSchema]
    [ComponentAlias("TileId")]
    [ComponentGroup(Group.History)]
    public partial struct TileId { }
}
