using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("CachePosition")]
    [ComponentGroup(Group.Temporary)]
    [Component(false)]
    public partial struct CachePosition : INewType
    {
        public fix3 Value;
    }
}
