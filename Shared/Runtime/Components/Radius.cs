using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema(0x9FD651785C8142E0UL)]
    [ComponentAlias("Radius")]
    [ComponentGroup(Group.History)]
    public partial struct Radius : INewType
    {
        public fix Value;
    }
}
