using Delta.ECS;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("ViewToken")]
    [ComponentGroup(Group.History)]
    public struct ViewToken
    {
        public string Value;
    }
}
