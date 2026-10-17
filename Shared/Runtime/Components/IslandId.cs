using Delta.ECS;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0xB73F5EC9A2416D08UL)]
    [ComponentAlias("IslandId")]
    [ComponentGroup(Group.History)]
    public struct IslandId
    {
        public int Value;
    }
}
