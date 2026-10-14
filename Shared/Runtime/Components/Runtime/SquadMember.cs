using Delta.ECS;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("SquadMember")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct SquadMember
    {
        public int SquadId;
    }
}
