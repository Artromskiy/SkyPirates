using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Health")]
    [ComponentGroup(Group.History)]
    public partial struct Health : INewType
    {
        public fix Value;
    }
}
