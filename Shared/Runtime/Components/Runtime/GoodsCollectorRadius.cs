using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("GoodsCollectorRadius")]
    [ComponentGroup(Group.History)]
    public partial struct GoodsCollectorRadius : INewType
    {
        public fix Value;
    }
}
