using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("ViewToken")]
    [ComponentGroup(Group.History)]
    public struct ViewToken
    {
        public string Value;
    }
}
