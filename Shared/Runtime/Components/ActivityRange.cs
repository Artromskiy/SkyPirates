using Delta.ECS;
using Delta;
using SlyPirates.Shared.Core.NewType;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0xF58475C35B5A76E4UL)]
    [ComponentAlias("ActivityRange")]
    [ComponentGroup(Group.History)]
    public partial struct ActivityRange : INewType
    {
        public fix Value;
    }
}
