using DVG.Json;
using System;

namespace DVG.SkyPirates.Shared.Models
{
    [JsonAsset]
    [Serializable]
    public partial class UnitModel
    {
        public float health;
        public float damage;
        public float speed;

        public float attackDistance;
        public float damageZone;
        public float bulletSpeed;
        public float reload;
        public float preAttack;
        public float postAttack;
    }
}