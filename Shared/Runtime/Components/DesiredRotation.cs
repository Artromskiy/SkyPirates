using Delta.ECS;
using Delta;

namespace SlyPirates.Shared.Components.Framed
{
    [ComponentSchema(0xC66B227BF5777917UL)]
    [ComponentAlias("DesiredRotation")]
    [ComponentGroup(Group.History)]
    public struct DesiredRotation
    {
        public fix Value;
    }
}
