using Delta.ECS;
using DVG.SkyPirates.Shared.Core.Ids.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Ids
{
    [Id(false)]
    [ComponentSchema]
    [ComponentAlias("RockId")]
    [ComponentGroup(Group.History)]
    public partial struct RockId { }
}
