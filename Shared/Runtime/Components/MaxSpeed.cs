using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0xF1B73157C23AE6BFUL)]
    [ComponentAlias("MaxSpeed")]
    [ComponentGroup(Group.History)]
    public partial struct MaxSpeed : INewType
    {
        public fix Value;
    }
}
