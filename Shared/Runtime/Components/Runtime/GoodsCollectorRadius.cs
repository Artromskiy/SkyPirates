using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("GoodsCollectorRadius")]
    [ComponentGroup(Group.History)]
    public partial struct GoodsCollectorRadius : INewType
    {
        public fix Value;
    }
}
