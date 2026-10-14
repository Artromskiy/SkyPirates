using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Direction")]
    [ComponentGroup(Group.History)]
    public partial struct Direction : INewType
    {
        public fix2 Value;
    }
}
