using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0x9F7F8738231D664EUL)]
    [ComponentAlias("Separation")]
    [ComponentGroup(Group.History)]
    public partial struct Separation : INewType
    {
        public fix Value;
    }
}
