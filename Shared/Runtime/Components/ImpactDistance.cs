using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0x9D0A7A0F533A2503UL)]
    [ComponentAlias("ImpactDistance")]
    [ComponentGroup(Group.History)]
    public partial struct ImpactDistance : INewType
    {
        public fix Value;
    }
}
