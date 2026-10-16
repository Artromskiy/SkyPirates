using Delta.ECS;
using Delta;

namespace SlyPirates.Shared.Components.Framed
{
    [ComponentSchema(0xC097C7A537A8EEB2UL)]
    [ComponentAlias("DesiredPosition")]
    [ComponentGroup(Group.History)]
    public struct DesiredPosition
    {
        public fix3 Value;
    }
}
