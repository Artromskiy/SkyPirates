using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("MaxSpeed")]
    [ComponentGroup(Group.History)]
    public partial struct MaxSpeed : INewType
    {
        public fix Value;
    }
}
