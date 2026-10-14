using Delta.ECS;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("TeamId")]
    [ComponentGroup(Group.History)]
    public partial struct TeamId : INewType
    {
        public int Value;
    }
}
