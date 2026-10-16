using Delta.ECS;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x0A20A07352E924D8UL)]
    [ComponentAlias("Fixation")]
    [ComponentGroup(Group.History)]
    public partial struct Fixation : INewType
    {
        public bool Value;
    }
}
