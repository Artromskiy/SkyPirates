using Delta.ECS;
using Delta;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
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
