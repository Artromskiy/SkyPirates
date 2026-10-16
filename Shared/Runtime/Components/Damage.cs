using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;


namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0xB7DEC3DE753B7A9FUL)]
    [ComponentAlias("Damage")]
    [ComponentGroup(Group.History)]
    public partial struct Damage : INewType
    {
        public fix Value;
    }
}
