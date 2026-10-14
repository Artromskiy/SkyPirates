using Delta.ECS;
using SkyPirates.Shared.Core.Ids.Attributes;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Ids
{
    [Id(false)]
    [ComponentSchema]
    [ComponentAlias("TreeId")]
    [ComponentGroup(Group.History)]
    public partial struct TreeId { }
}
