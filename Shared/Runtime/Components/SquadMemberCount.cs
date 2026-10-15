using Delta.ECS;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema(0x0167B79CCC5AC89EUL)]
    [ComponentAlias("SquadMemberCount")]
    [ComponentGroup(Group.Temporary)]
    public partial struct SquadMemberCount : INewType
    {
        public int Value;
    }
}
