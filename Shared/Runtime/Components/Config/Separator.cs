using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Separator")]
    [ComponentGroup(Group.History)]
    public struct Separator
    {
        public fix Radius;
        public fix Coefficient;
    }
}
