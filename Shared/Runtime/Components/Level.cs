using Delta.ECS;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0xE1687395DD953020UL)]
    [ComponentAlias("Level")]
    [ComponentGroup(Group.History)]
    public partial struct Level : INewType
    {
        public int Value;
    }
}
