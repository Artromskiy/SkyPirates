using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Position")]
    [ComponentGroup(Group.History)]
    public partial struct Position : INewType
    {
        public fix3 Value;
    }
}
