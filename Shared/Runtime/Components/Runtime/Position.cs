using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Position")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public partial struct Position : INewType
    {
        public fix3 Value;
    }
}
