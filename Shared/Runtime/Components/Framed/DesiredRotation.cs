using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("DesiredRotation")]
    [ComponentGroup(Group.History)]
    public struct DesiredRotation
    {
        public fix Value;
    }
}
