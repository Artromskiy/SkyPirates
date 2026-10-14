using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("AutoHeal")]
    [ComponentGroup(Group.History)]
    public struct AutoHeal
    {
        public fix HealPerSecond;
        public fix HealDelay;

        public fix HealLoadPercent;
    }
}
