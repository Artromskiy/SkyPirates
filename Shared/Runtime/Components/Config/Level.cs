using Delta.ECS;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Level")]
    [ComponentGroup(Group.History)]
    public partial struct Level : INewType
    {
        public int Value;
    }
}
