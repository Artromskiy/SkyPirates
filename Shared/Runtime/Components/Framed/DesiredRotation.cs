using Delta.ECS;
using Delta;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("DesiredRotation")]
    [ComponentGroup(Group.History)]
    public struct DesiredRotation
    {
        public fix Value;
    }
}
