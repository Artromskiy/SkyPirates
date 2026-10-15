using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x8D01E83A9E7CDD6DUL)]
    [ComponentAlias("Source")]
    [ComponentGroup(Group.History)]
    public struct Source
    {
        public Entity Entity;
    }
}
