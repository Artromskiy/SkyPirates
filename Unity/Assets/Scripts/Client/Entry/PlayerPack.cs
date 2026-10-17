using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ids;
using System;
using System.Collections.Generic;

namespace SlyPirates.Client.Entry
{
    public sealed class PlayerPack
    {
        private readonly List<UnitId> _teamCards;
        private readonly List<UnitId> _collectionCards;

        public bool IsTeamComplete => _teamCards.Count == UnitDeck.Capacity;

        private PlayerPack(UnitId[] ownedCards, UnitDeck startingDeck)
        {
            if (!startingDeck.IsComplete)
                throw new ArgumentException("The starting deck must contain four cards.", nameof(startingDeck));

            List<UnitId> availableCards = new(ownedCards);
            _teamCards = new List<UnitId>(UnitDeck.Capacity)
            {
                startingDeck.First,
                startingDeck.Second,
                startingDeck.Third,
                startingDeck.Fourth,
            };

            _collectionCards = new List<UnitId>(ownedCards.Length - UnitDeck.Capacity);
            for (int index = 0; index < availableCards.Count; index++)
            {
                UnitId unitId = availableCards[index];
                if (!_teamCards.Contains(unitId))
                    _collectionCards.Add(unitId);
            }
        }

        public static PlayerPack CreateStarter()
        {
            UnitDeck startingDeck = new(
                UnitId.Constants.Rogue,
                UnitId.Constants.Buccaneer,
                UnitId.Constants.Militia,
                UnitId.Constants.Pirate);
            ReadOnlySpan<UnitId> allUnitIds = UnitId.Constants.AllIds;
            UnitId[] ownedCards = allUnitIds.ToArray();
            return new PlayerPack(ownedCards, startingDeck);
        }

        public UnitId[] GetTeamCards() => _teamCards.ToArray();

        public UnitId[] GetCollectionCards() => _collectionCards.ToArray();

        public UnitDeck GetTeamDeck()
        {
            if (!IsTeamComplete)
                throw new InvalidOperationException("The team must contain four cards before starting the game.");

            return new UnitDeck(_teamCards[0], _teamCards[1], _teamCards[2], _teamCards[3]);
        }

        public bool MoveToTeam(UnitId unitId)
        {
            if (_teamCards.Count >= UnitDeck.Capacity || !_collectionCards.Remove(unitId))
                return false;

            _teamCards.Add(unitId);
            return true;
        }

        public bool MoveToCollection(UnitId unitId)
        {
            if (!_teamCards.Remove(unitId))
                return false;

            _collectionCards.Add(unitId);
            return true;
        }
    }
}
