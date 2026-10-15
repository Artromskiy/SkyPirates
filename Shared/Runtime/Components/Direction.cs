using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x4275CCFB0E7CFAE9UL)]
    [ComponentAlias("Direction")]
    [ComponentGroup(Group.History)]
    public partial struct Direction : INewType
    {
        public fix2 Value;
    }
}
