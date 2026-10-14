using Delta.ECS;
using Delta;
using SkyPirates.Shared.Core.NewType;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("CachePosition")]
    [ComponentGroup(Group.Temporary)]
    public partial struct CachePosition : INewType
    {
        public fix3 Value;
    }
}
