using Delta.ECS;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Fixation")]
    [ComponentGroup(Group.History)]
    public partial struct Fixation : INewType
    {
        public bool Value;
    }
}
