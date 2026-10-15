using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema(0x08FBC3FC7C77F884UL)]
    [ComponentAlias("TargetSearchDistance")]
    [ComponentGroup(Group.History)]
    public partial struct TargetSearchDistance : INewType
    {
        public fix Value;
    }
}
