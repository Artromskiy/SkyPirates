using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("Radius")]
    [ComponentGroup(Group.History)]
    public partial struct Radius : INewType
    {
        public fix Value;
    }
}
