using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Framed
{
    [ComponentSchema(0x5DDED9CDB601690FUL)]
    [ComponentAlias("TargetSearchPosition")]
    [ComponentGroup(Group.Temporary)]
    public partial struct TargetSearchPosition : INewType
    {
        public fix3 Value;
    }
}
