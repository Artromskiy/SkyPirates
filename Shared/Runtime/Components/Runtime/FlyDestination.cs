using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
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
