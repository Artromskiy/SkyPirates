using Delta.ECS;
using SlyPirates.Shared.Core.Ids.Attributes;
using SlyPirates.Shared.Components;

namespace SlyPirates.Shared.Ids
{
    [Id(false)]
    [ComponentSchema(0xA11CF917F4947C98UL)]
    [ComponentAlias("RockId")]
    [ComponentGroup(Group.History)]
    public partial struct RockId { }
}
