using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Collide")]
    [ComponentGroup(Group.History)]
    public readonly struct Collide { }
}
