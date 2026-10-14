using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Rotation")]
    [ComponentGroup(Group.History)]
    public partial struct Rotation : INewType
    {
        public fix Value;
    }
}
