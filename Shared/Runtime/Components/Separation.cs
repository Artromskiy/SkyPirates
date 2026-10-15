using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema(0x9F7F8738231D664EUL)]
    [ComponentAlias("Separation")]
    [ComponentGroup(Group.History)]
    public partial struct Separation : INewType
    {
        public fix Value;
    }
}
