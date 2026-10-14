using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("AutoHeal")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct AutoHeal
    {
        public fix HealPerSecond;
        public fix HealDelay;

        public fix HealLoadPercent;
    }
}
