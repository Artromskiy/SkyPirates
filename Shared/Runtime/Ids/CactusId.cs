using Delta.ECS;
using DVG.SkyPirates.Shared.Core.Ids.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Ids
{
    [Id(false)]
    [ComponentSchema]
    [ComponentAlias("CactusId")]
    [ComponentGroup(Group.History)]
    public partial struct CactusId { }
}
