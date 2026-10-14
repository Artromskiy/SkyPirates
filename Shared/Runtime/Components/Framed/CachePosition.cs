using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("CachePosition")]
    [ComponentGroup(Group.Temporary)]
    public partial struct CachePosition : INewType
    {
        public fix3 Value;
    }
}
