using Delta.ECS;
using DVG.Components.Attributes;
using DVG.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Level")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public partial struct Level : INewType
    {
        public int Value;
    }
}
