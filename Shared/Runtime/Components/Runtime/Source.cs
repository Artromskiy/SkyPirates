using Delta.ECS;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Source")]
    [ComponentGroup(Group.History)]
    public struct Source
    {
        public Entity Entity;
    }
}
