using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Projectile")]
    [ComponentGroup(Group.History)]
    public struct Projectile
    {
        public Entity Target;
        public fix3 StartPosition;
    }
}
