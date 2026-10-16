using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0x85D46A3B80A80A02UL)]
    [ComponentAlias("MaxHealth")]
    [ComponentGroup(Group.History)]
    public partial struct MaxHealth : INewType
    {
        public fix Value;
    }
}
