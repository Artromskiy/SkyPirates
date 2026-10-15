using Delta.ECS;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema(0xE1687395DD953020UL)]
    [ComponentAlias("Level")]
    [ComponentGroup(Group.History)]
    public partial struct Level : INewType
    {
        public int Value;
    }
}
