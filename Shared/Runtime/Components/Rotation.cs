using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x4FE9B88959B93E3EUL)]
    [ComponentAlias("Rotation")]
    [ComponentGroup(Group.History)]
    public partial struct Rotation : INewType
    {
        public fix Value;
    }
}
