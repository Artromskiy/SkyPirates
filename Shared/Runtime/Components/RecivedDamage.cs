using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Framed
{
    [ComponentSchema(0xA7AE4DE7E976583AUL)]
    [ComponentAlias("RecivedDamage")]
    [ComponentGroup(Group.Temporary)]
    public partial struct RecivedDamage : INewType
    {
        public fix Value;
    }
}
