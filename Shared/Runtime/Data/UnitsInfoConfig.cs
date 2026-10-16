using SlyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SlyPirates.Shared.Data
{
    public sealed class UnitsInfoConfig : Dictionary<UnitId, UnitInfoData> { }

    public sealed class UnitInfoData
    {
        public UnitId UnitId;
        public int RumPrice;
    }
}
