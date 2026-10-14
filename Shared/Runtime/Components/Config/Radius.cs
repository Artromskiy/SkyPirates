using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Radius")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public partial struct Radius : INewType
    {
        public fix Value;
    }
}
