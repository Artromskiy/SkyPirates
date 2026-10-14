using Delta.ECS;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("SquadMemberCount")]
    [ComponentGroup(Group.Temporary)]
    public partial struct SquadMemberCount : INewType
    {
        public int Value;
    }
}
