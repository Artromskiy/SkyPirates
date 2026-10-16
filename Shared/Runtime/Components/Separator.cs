using Delta.ECS;
using Delta;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0x00AB8D1A9CB1C8D5UL)]
    [ComponentAlias("Separator")]
    [ComponentGroup(Group.History)]
    public struct Separator
    {
        public fix Radius;
        public fix Coefficient;
    }
}
