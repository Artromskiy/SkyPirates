using DVG.Json;
using System;

namespace DVG.SkyPirates.Shared.Models
{
    [JsonAsset]
    [Serializable]
    public partial class FortModel
    {
        public int health;
        public int cannons;
        public int damage;
        public float reloadTime;
        public float attackRadius;
        public float projectileSpeed;
        public int maxHold;
        public int level;
        public int maxLevel;
    }
}
