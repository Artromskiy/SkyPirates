using Delta.ECS;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Fixation")]
    [ComponentGroup(Group.History)]
    public partial struct Fixation : INewType
    {
        public bool Value;
    }
}
