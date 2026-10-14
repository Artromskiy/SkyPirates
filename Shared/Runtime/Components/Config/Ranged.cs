using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
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
