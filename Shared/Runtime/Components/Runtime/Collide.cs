using Delta.ECS;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Collide")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public readonly struct Collide { }
}
