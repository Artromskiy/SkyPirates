using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0x08FBC3FC7C77F884UL)]
    [ComponentAlias("TargetSearchDistance")]
    [ComponentGroup(Group.History)]
    public partial struct TargetSearchDistance : INewType
    {
        public fix Value;
    }
}
