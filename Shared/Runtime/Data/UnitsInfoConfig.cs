using SkyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SkyPirates.Shared.Data
{
    public sealed class UnitsInfoConfig : Dictionary<UnitId, UnitInfoData> { }

    public sealed class UnitInfoData
    {
        public UnitId UnitId;
        public int RumPrice;
    }
}
