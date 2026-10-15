using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema(0x5DDED9CDB601690FUL)]
    [ComponentAlias("TargetSearchPosition")]
    [ComponentGroup(Group.Temporary)]
    public partial struct TargetSearchPosition : INewType
    {
        public fix3 Value;
    }
}
