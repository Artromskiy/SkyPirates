using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Speed")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct Speed
    {
        public fix Value;
    }
}
