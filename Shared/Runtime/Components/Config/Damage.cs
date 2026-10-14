using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.NewType;
using DVG.SkyPirates.Shared.Components;


namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Damage")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public partial struct Damage : INewType
    {
        public fix Value;
    }
}
