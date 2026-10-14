using Delta.ECS;
using DVG.Components.Attributes;
using DVG.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("GoodsAmount")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public partial struct GoodsAmount : INewType
    {
        public int Value;
    }
}
