using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0x9FD651785C8142E0UL)]
    [ComponentAlias("Radius")]
    [ComponentGroup(Group.History)]
    public partial struct Radius : INewType
    {
        public fix Value;
    }
}
