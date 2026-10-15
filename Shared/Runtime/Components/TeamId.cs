using Delta.ECS;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x000700D99010F434UL)]
    [ComponentAlias("TeamId")]
    [ComponentGroup(Group.History)]
    public partial struct TeamId : INewType
    {
        public int Value;
    }
}
