using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema(0x620B9E215CE58422UL)]
    [ComponentAlias("CachePosition")]
    [ComponentGroup(Group.Temporary)]
    public partial struct CachePosition : INewType
    {
        public fix3 Value;
    }
}
