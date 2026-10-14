using Delta.ECS;
using Delta;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("FlyDestination")]
    [ComponentGroup(Group.History)]
    public struct FlyDestination
    {
        public fix3 StartPosition;
        public fix3 EndPosition;
    }
}
