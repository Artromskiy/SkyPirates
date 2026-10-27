using SlyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SlyPirates.Shared.Data
{
    public sealed class UnitsInfoConfig : Dictionary<UnitId, UnitInfoData> { }

    public sealed class UnitInfoData
    {
        public UnitId UnitId;
        public string LocalizationKey = string.Empty;
        public int Cost;
        public string PrefabAddressable = string.Empty;
        public string IconAddressable = string.Empty;
    }
}
