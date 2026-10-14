using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Rotation")]
    [ComponentGroup(Group.History)]
    public partial struct Rotation : INewType
    {
        public fix Value;
    }
}
