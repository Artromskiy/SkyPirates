using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema(0xA7AE4DE7E976583AUL)]
    [ComponentAlias("RecivedDamage")]
    [ComponentGroup(Group.Temporary)]
    public partial struct RecivedDamage : INewType
    {
        public fix Value;
    }
}
