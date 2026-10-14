using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("RecivedDamage")]
    [ComponentGroup(Group.Temporary)]
    public partial struct RecivedDamage : INewType
    {
        public fix Value;
    }
}
