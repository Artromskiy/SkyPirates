using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Direction")]
    [ComponentGroup(Group.History)]
    public partial struct Direction : INewType
    {
        public fix2 Value;
    }
}
