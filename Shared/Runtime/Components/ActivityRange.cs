using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema(0xF58475C35B5A76E4UL)]
    [ComponentAlias("ActivityRange")]
    [ComponentGroup(Group.History)]
    public partial struct ActivityRange : INewType
    {
        public fix Value;
    }
}
