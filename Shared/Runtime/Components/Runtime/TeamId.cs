using Delta.ECS;
using DVG.Components.Attributes;
using DVG.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("TeamId")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public partial struct TeamId : INewType
    {
        public int Value;
    }
}
