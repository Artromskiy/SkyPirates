using Delta.ECS;
using Delta;

namespace SlyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x4CD0683D2A0B1ECDUL)]
    [ComponentAlias("AutoHeal")]
    [ComponentGroup(Group.History)]
    public struct AutoHeal
    {
        public fix HealPerSecond;
        public fix HealDelay;

        public fix HealLoadPercent;
    }
}
