using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("MaxSpeed")]
    [ComponentGroup(Group.History)]
    public partial struct MaxSpeed : INewType
    {
        public fix Value;
    }
}
