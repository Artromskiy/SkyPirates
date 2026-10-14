using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("TargetSearchDistance")]
    [ComponentGroup(Group.History)]
    public partial struct TargetSearchDistance : INewType
    {
        public fix Value;
    }
}
