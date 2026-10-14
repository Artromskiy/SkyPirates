using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("ActivityRange")]
    [ComponentGroup(Group.History)]
    public partial struct ActivityRange : INewType
    {
        public fix Value;
    }
}
