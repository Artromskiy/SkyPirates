using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Core.NewType;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x4FE9B88959B93E3EUL)]
    [ComponentAlias("Rotation")]
    [ComponentGroup(Group.History)]
    public partial struct Rotation : INewType
    {
        public fix Value;
    }
}
