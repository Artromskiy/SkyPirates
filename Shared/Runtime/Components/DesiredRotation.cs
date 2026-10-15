using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema(0xC66B227BF5777917UL)]
    [ComponentAlias("DesiredRotation")]
    [ComponentGroup(Group.History)]
    public struct DesiredRotation
    {
        public fix Value;
    }
}
