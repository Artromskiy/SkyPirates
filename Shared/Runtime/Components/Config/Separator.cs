using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Separator")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct Separator
    {
        public fix Radius;
        public fix Coefficient;
    }
}
