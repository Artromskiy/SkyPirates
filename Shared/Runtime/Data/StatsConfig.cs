using SkyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SkyPirates.Shared.Data
{
    public sealed class UnitsStatsConfig : Dictionary<UnitId, ComponentDataSet> { }
    public sealed class RocksStatsConfig : Dictionary<RockId, ComponentDataSet> { }
    public sealed class TreesStatsConfig : Dictionary<TreeId, ComponentDataSet> { }
    public sealed class GoodsStatsConfig : Dictionary<GoodsId, ComponentDataSet> { }
    public sealed class CactusesStatsConfig : Dictionary<CactusId, ComponentDataSet> { }
}
