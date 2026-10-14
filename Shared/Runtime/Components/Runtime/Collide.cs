using Delta.ECS;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Collide")]
    [ComponentGroup(Group.History)]
    public readonly struct Collide { }
}
