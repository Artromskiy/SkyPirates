using Delta.ECS;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("Target")]
    [ComponentGroup(Group.History)]
    [ComponentGroup(Group.Temporary)]
    [Component(false)]
    public struct Target
    {
        public Entity? Entity;
    }
}
