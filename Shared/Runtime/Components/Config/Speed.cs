using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Speed")]
    [ComponentGroup(Group.History)]
    public struct Speed
    {
        public fix Value;
    }
}
