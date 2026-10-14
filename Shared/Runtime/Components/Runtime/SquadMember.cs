using Delta.ECS;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("SquadMember")]
    [ComponentGroup(Group.History)]
    public struct SquadMember
    {
        public int SquadId;
    }
}
