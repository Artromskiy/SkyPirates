using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x5E25F00897E1DBABUL)]
    [ComponentAlias("Position")]
    [ComponentGroup(Group.History)]
    public partial struct Position : INewType
    {
        public fix3 Value;
    }
}
