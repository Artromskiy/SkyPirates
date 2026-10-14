using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("DesiredPosition")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct DesiredPosition
    {
        public fix3 Value;
    }
}
