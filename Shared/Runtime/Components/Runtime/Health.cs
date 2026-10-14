using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Health")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public partial struct Health : INewType
    {
        public fix Value;
    }
}
