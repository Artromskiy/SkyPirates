using Delta.ECS;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x000700D99010F434UL)]
    [ComponentAlias("TeamId")]
    [ComponentGroup(Group.History)]
    public partial struct TeamId : INewType
    {
        public int Value;
    }
}
