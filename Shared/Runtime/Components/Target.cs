using Delta.ECS;
using SlyPirates.Shared.Components.Runtime;

namespace SlyPirates.Shared.Components.Framed
{
    [ComponentSchema(0x5C009C7EB74ACAD0UL)]
    [ComponentAlias("Target")]
    [ComponentGroup(Group.History)]
    [ComponentGroup(Group.Temporary)]
    public struct Target
    {
        public SyncId? SyncId;
    }
}
