using Delta.ECS;
using Delta;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
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
