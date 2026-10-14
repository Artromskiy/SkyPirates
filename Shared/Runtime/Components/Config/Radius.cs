using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Radius")]
    [ComponentGroup(Group.History)]
    public partial struct Radius : INewType
    {
        public fix Value;
    }
}
