using Delta.ECS;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("TeamId")]
    [ComponentGroup(Group.History)]
    public partial struct TeamId : INewType
    {
        public int Value;
    }
}
