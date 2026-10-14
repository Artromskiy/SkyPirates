using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("SquadMember")]
    [ComponentGroup(Group.History)]
    public struct SquadMember
    {
        public int SquadId;
    }
}
