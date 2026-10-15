using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema(0x39CDBEAC8FE6E741UL)]
    [ComponentAlias("Ranged")]
    [ComponentGroup(Group.History)]
    public struct Ranged
    {
        public float ProjectileSpeed;
        public string ProjectileViewToken;
    }
}
