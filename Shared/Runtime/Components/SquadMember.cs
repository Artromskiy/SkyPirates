using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x19F395FF16C393E4UL)]
    [ComponentAlias("SquadMember")]
    [ComponentGroup(Group.History)]
    public struct SquadMember
    {
        public SyncId SquadId;
    }
}
