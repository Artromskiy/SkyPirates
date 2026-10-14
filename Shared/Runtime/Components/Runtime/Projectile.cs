using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Projectile")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct Projectile
    {
        public Entity Target;
        public fix3 StartPosition;
    }
}
