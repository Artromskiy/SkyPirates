using Delta.ECS;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Source")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct Source
    {
        public Entity Entity;
    }
}
