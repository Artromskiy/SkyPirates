using DVG.SkyPirates.Shared.Ids;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Data
{
    public sealed class UnitsInfoConfig : Dictionary<UnitId, UnitInfoData> { }

    public sealed class UnitInfoData
    {
        public UnitId UnitId;
        public int RumPrice;
    }
}
