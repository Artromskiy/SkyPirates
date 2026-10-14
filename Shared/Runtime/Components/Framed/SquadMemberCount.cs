using Delta.ECS;
using DVG.Components.Attributes;
using DVG.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("SquadMemberCount")]
    [ComponentGroup(Group.Temporary)]
    [Component(false)]
    public partial struct SquadMemberCount : INewType
    {
        public int Value;
    }
}
