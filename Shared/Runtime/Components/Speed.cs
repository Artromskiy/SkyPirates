using Delta.ECS;
using Delta;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema(0xC3EF8240429044E5UL)]
    [ComponentAlias("Speed")]
    [ComponentGroup(Group.History)]
    public struct Speed
    {
        public fix Value;
    }
}
