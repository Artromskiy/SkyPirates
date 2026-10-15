using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x5E25F00897E1DBABUL)]
    [ComponentAlias("Position")]
    [ComponentGroup(Group.History)]
    public partial struct Position : INewType
    {
        public fix3 Value;
    }
}
