using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("FlyDestination")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct FlyDestination
    {
        public fix3 StartPosition;
        public fix3 EndPosition;
    }
}
