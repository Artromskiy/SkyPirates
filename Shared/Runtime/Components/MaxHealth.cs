using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema(0x85D46A3B80A80A02UL)]
    [ComponentAlias("MaxHealth")]
    [ComponentGroup(Group.History)]
    public partial struct MaxHealth : INewType
    {
        public fix Value;
    }
}
