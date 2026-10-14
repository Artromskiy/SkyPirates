using Delta.ECS;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("Target")]
    [ComponentGroup(Group.History)]
    [ComponentGroup(Group.Temporary)]
    public struct Target
    {
        public Entity? Entity;
    }
}
