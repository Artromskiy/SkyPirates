using Delta.ECS;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Framed
{
    [ComponentSchema(0x0167B79CCC5AC89EUL)]
    [ComponentAlias("SquadMemberCount")]
    [ComponentGroup(Group.Temporary)]
    public partial struct SquadMemberCount : INewType
    {
        public int Value;
    }
}
