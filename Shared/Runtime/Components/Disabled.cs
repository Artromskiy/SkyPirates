using Delta.ECS;

namespace SlyPirates.Shared.Components.Framed
{
    [ComponentSchema(0x1EC3F4F97C15182DUL)]
    [ComponentAlias("Disabled")]
    [ComponentGroup(Group.History)]
    public readonly struct Disabled
    {
        // keep it so Disabled is treated as component
        private readonly int _;
    }
}
