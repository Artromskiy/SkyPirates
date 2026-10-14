using Delta.ECS;
using Delta;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
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
