using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;


namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Damage")]
    [ComponentGroup(Group.History)]
    public partial struct Damage : INewType
    {
        public fix Value;
    }
}
