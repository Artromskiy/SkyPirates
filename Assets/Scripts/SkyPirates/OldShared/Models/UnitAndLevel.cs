using DVG.Json;
using DVG.SkyPirates.OldShared.Ids;
using System;

namespace DVG.SkyPirates.OldShared.Models
{
    [JsonAsset]
    [Serializable]
    public partial class UnitAndLevel
    {
        public UnitId unitId;
        public int level;

        public UnitAndLevel(UnitId unitId, int level)
        {
            this.unitId = unitId;
            this.level = level;
        }
    }
}
