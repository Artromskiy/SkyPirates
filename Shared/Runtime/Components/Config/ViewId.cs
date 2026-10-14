using Delta.ECS;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("ViewToken")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct ViewToken
    {
        public string Value;
    }
}
