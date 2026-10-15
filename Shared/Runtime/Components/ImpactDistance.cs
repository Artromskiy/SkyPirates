using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema(0x9D0A7A0F533A2503UL)]
    [ComponentAlias("ImpactDistance")]
    [ComponentGroup(Group.History)]
    public partial struct ImpactDistance : INewType
    {
        public fix Value;
    }
}
