using Delta.ECS;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Ranged")]
    [ComponentGroup(Group.History)]
    public struct Ranged
    {
        public float ProjectileSpeed;
        public string ProjectileViewToken;
    }
}
