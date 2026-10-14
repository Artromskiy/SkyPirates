using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("TargetSearchPosition")]
    [ComponentGroup(Group.Temporary)]
    [Component(false)]
    public partial struct TargetSearchPosition : INewType
    {
        public fix3 Value;
    }
}
