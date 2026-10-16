using Delta.ECS;

namespace SlyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x8D01E83A9E7CDD6DUL)]
    [ComponentAlias("Source")]
    [ComponentGroup(Group.History)]
    public struct Source
    {
        public SyncId SyncId;
    }
}
