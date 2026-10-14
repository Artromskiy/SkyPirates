using Delta.ECS;
using Delta;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Speed")]
    [ComponentGroup(Group.History)]
    public struct Speed
    {
        public fix Value;
    }
}
