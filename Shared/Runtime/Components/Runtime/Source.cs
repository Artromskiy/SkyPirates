using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Source")]
    [ComponentGroup(Group.History)]
    public struct Source
    {
        public Entity Entity;
    }
}
