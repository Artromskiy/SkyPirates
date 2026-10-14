using Delta.ECS;
using DVG.Components.Attributes;
using DVG.Ids.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Ids
{
    [Id(false)]
    [ComponentSchema]
    [ComponentAlias("TreeId")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public partial struct TreeId { }
}
