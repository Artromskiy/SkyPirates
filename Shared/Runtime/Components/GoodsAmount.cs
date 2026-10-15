using Delta.ECS;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema(0x55F071E925356D84UL)]
    [ComponentAlias("GoodsAmount")]
    [ComponentGroup(Group.History)]
    public partial struct GoodsAmount : INewType
    {
        public int Value;
    }
}
