using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0xE5DC9B0C9498A00EUL)]
    [ComponentAlias("Health")]
    [ComponentGroup(Group.History)]
    public partial struct Health : INewType
    {
        public fix Value;
    }
}
