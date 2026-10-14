using Delta.ECS;
using DVG.Components.Attributes;
using DVG.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("Fixation")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public partial struct Fixation : INewType
    {
        public bool Value;
    }
}
