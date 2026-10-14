using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("DesiredRotation")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct DesiredRotation
    {
        public fix Value;
    }
}
