using Delta.Netcode;
using SlyPirates.Shared.Data;

namespace SlyPirates.Shared.Commands
{
    [CommandSchema(0x00B081EA67211ACAUL, Predicted = false)]
    public struct SpawnSquadCommand
    {
        public EntityParameters CreationData;
        public UnitDeck Deck;
    }
}
