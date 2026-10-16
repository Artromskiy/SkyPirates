using Delta.ECS;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0xC3F600D83AC9B1D8UL)]
    [ComponentAlias("ViewToken")]
    [ComponentGroup(Group.History)]
    public struct ViewToken
    {
        public string Value;
    }
}
