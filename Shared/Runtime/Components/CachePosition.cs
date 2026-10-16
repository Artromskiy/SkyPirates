using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Components.Framed
{
    [ComponentSchema(0x620B9E215CE58422UL)]
    [ComponentAlias("CachePosition")]
    [ComponentGroup(Group.Temporary)]
    public partial struct CachePosition : INewType
    {
        public fix3 Value;
    }
}
