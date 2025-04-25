using DVG.Json;
using System;

namespace DVG.SkyPirates.Shared.Models
{
    [JsonAsset]
    [Serializable]
    public partial class SquadModel
    {
        public UnitAndLevel[] cards;

        public SquadModel(UnitAndLevel[] cards)
        {
            this.cards = cards;
        }
    }
}