using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;


namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema(0xB7DEC3DE753B7A9FUL)]
    [ComponentAlias("Damage")]
    [ComponentGroup(Group.History)]
    public partial struct Damage : INewType
    {
        public fix Value;
    }
}
