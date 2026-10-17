using Delta.ECS;
using SlyPirates.Shared.Data;

namespace SlyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x98B551D3662E64CAUL)]
    [ComponentAlias("Squad")]
    [ComponentGroup(Group.History)]
    public struct Squad
    {
        public UnitDeck Deck;
    }
}
