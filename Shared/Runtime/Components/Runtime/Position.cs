using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Position")]
    [ComponentGroup(Group.History)]
    public partial struct Position : INewType
    {
        public fix3 Value;
    }
}
