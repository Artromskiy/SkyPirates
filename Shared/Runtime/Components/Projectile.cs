using Delta.ECS;
using Delta;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0xFD545D4B7B5D50FFUL)]
    [ComponentAlias("Projectile")]
    [ComponentGroup(Group.History)]
    public struct Projectile
    {
        public SyncId Target;
        public fix3 StartPosition;
    }
}
