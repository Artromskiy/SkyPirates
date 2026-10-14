using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("DesiredPosition")]
    [ComponentGroup(Group.History)]
    public struct DesiredPosition
    {
        public fix3 Value;
    }
}
