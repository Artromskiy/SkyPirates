using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
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
