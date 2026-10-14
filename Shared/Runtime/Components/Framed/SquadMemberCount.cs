using Delta.ECS;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("SquadMemberCount")]
    [ComponentGroup(Group.Temporary)]
    public partial struct SquadMemberCount : INewType
    {
        public int Value;
    }
}
