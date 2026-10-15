using Delta.ECS;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x0A20A07352E924D8UL)]
    [ComponentAlias("Fixation")]
    [ComponentGroup(Group.History)]
    public partial struct Fixation : INewType
    {
        public bool Value;
    }
}
