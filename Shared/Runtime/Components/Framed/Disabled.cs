using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("Disabled")]
    [ComponentGroup(Group.History)]
    public struct Disabled
    {
        private bool _hidden;
    }
}
