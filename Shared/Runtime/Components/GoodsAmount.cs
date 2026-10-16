using Delta.ECS;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0x55F071E925356D84UL)]
    [ComponentAlias("GoodsAmount")]
    [ComponentGroup(Group.History)]
    public partial struct GoodsAmount : INewType
    {
        public int Value;
    }
}
