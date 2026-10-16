using Delta.ECS;
using Delta;

namespace SlyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x6A3B5C9A7DA0F83BUL)]
    [ComponentAlias("FlyDestination")]
    [ComponentGroup(Group.History)]
    public struct FlyDestination
    {
        public fix3 StartPosition;
        public fix3 EndPosition;
    }
}
