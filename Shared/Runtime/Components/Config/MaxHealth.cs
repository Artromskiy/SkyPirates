using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("MaxHealth")]
    [ComponentGroup(Group.History)]
    public partial struct MaxHealth : INewType
    {
        public fix Value;
    }
}
