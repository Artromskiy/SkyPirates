using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("ImpactDistance")]
    [ComponentGroup(Group.History)]
    public partial struct ImpactDistance : INewType
    {
        public fix Value;
    }
}
