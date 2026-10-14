using Delta.ECS;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Squad")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct Squad { }
}
