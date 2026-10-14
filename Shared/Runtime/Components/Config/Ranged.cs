using Delta.ECS;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Ranged")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct Ranged
    {
        public float ProjectileSpeed;
        public string ProjectileViewToken;
    }
}
