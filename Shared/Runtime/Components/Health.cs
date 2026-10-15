using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0xE5DC9B0C9498A00EUL)]
    [ComponentAlias("Health")]
    [ComponentGroup(Group.History)]
    public partial struct Health : INewType
    {
        public fix Value;
    }
}
