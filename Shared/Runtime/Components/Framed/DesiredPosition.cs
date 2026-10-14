using Delta.ECS;
using Delta;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("DesiredPosition")]
    [ComponentGroup(Group.History)]
    public struct DesiredPosition
    {
        public fix3 Value;
    }
}
