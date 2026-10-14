using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Separation")]
    [ComponentGroup(Group.History)]
    public partial struct Separation : INewType
    {
        public fix Value;
    }
}
