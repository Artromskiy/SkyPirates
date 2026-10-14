using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("RecivedDamage")]
    [ComponentGroup(Group.Temporary)]
    [Component(false)]
    public partial struct RecivedDamage : INewType
    {
        public fix Value;
    }
}
