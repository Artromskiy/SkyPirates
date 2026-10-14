using Delta.ECS;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("GoodsAmount")]
    [ComponentGroup(Group.History)]
    public partial struct GoodsAmount : INewType
    {
        public int Value;
    }
}
