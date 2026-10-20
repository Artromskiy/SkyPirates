using Delta.ECS;
using SlyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SlyPirates.Shared.Data
{
    public sealed class UnitsStatsConfig : Dictionary<UnitId, EntityData> { }
    public sealed class RocksStatsConfig : Dictionary<RockId, EntityData> { }
    public sealed class TreesStatsConfig : Dictionary<TreeId, EntityData> { }
    public sealed class GoodsStatsConfig : Dictionary<GoodsId, EntityData> { }
    public sealed class CactusesStatsConfig : Dictionary<CactusId, EntityData> { }
}
