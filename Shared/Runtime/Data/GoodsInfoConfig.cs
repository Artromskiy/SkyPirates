using SlyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SlyPirates.Shared.Data
{
    public sealed class GoodsInfoConfig : Dictionary<GoodsId, GoodsInfoData> { }

    public sealed class GoodsInfoData
    {
        public GoodsId GoodsId;
        public string PrefabAddressable = string.Empty;
        public string IconAddressable = string.Empty;
    }
}
