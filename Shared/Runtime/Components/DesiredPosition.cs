using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema(0xC097C7A537A8EEB2UL)]
    [ComponentAlias("DesiredPosition")]
    [ComponentGroup(Group.History)]
    public struct DesiredPosition
    {
        public fix3 Value;
    }
}
