using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.Components;
using DVG.Core.Collections;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Systems
{
    // crazy stuff
    public sealed class GoodsCollectorSystem : IDeltaTickableExecutor
    {
        private const int SquareSize = 5;


        private readonly Lookup2D<List<DropRef>> _partitioning = new();
        private readonly Lookup<BestCollector> _bestCollectors = new();
        private readonly Lookup<List<GoodsData>> _collectorsDrops = new();

        private readonly List<Entity> _removeDrops = new();
        private readonly IWorldHistory _history;

        private Query? _dropsDescCache;

        private Query _dropsDesc => _dropsDescCache ??= _world
            .WhereAll<GoodsId, SyncId, Position, GoodsAmount, MaxSpeed>().WhereNone<FlyDestination>().Alive()
            .NotDisabled();

        // if something is collector and collectable at same time => heat death of the universe
        private Query? _collectorsDescCache;

        private Query _collectorsDesc => _collectorsDescCache ??=
            _world.WhereAll<SyncId, Position, GoodsDrop, GoodsCollectorRadius>().Alive().NotDisabled();

        private readonly World _world;

        public GoodsCollectorSystem(World world, IWorldHistory history)
        {
            _world = world;
            _history = history;
        }

        public void Tick(int tick, fix deltaTime)
        {
            _partitioning.Clear();
            _bestCollectors.Clear();
            _collectorsDrops.Clear();
            _removeDrops.Clear();

            var partitioning = _partitioning;
            var dropsDesc = _dropsDesc;
            _world.ForEach<Lookup2D<List<DropRef>>, SyncId, Position>(in dropsDesc, ref partitioning,
                static (ref Lookup2D<List<DropRef>> grid, in SyncId syncId, in Position position) =>
                {
                    var posXZ = ((fix3)position).xz;
                    var quad = GetQuantizedSquare(posXZ);
                    if (!grid.TryGetValue(quad.x, quad.y, out var list))
                    {
                        grid[quad.x, quad.y] = list = new List<DropRef>(8);
                    }

                    list.Add(new DropRef { SyncId = syncId.Value, PositionXZ = posXZ });
                }).Invoke(ref partitioning);

            (Lookup2D<List<DropRef>> Grid, Lookup<BestCollector> Best) bestState = (_partitioning, _bestCollectors);
            var collectorsDesc = _collectorsDesc;
            _world
                .ForEach<(Lookup2D<List<DropRef>> Grid, Lookup<BestCollector> Best), SyncId, Position,
                    GoodsCollectorRadius>(in collectorsDesc, ref bestState,
                    static (ref (Lookup2D<List<DropRef>> Grid, Lookup<BestCollector> Best) state,
                        in SyncId collectorId, in Position collectorPos, in GoodsCollectorRadius radius) =>
                    {
                        fix2 center = ((fix3)collectorPos).xz;
                        fix searchRadius = radius;
                        fix sqrSearchRadius = searchRadius * searchRadius;
                        var range = new fix2(searchRadius, searchRadius);
                        var min = GetQuantizedSquare(center - range);
                        var max = GetQuantizedSquare(center + range);

                        for (int y = min.y; y <= max.y; y++)
                        for (int x = min.x; x <= max.x; x++)
                        {
                            if (!state.Grid.TryGetValue(x, y, out var drops))
                            {
                                continue;
                            }

                            for (int i = 0; i < drops.Count; i++)
                            {
                                var drop = drops[i];
                                var sqrDist = fix2.SqrDistance(drop.PositionXZ, center);
                                if (sqrDist > sqrSearchRadius)
                                {
                                    continue;
                                }

                                int dropId = drop.SyncId;
                                if (!state.Best.TryGetValue(dropId, out var current) ||
                                    sqrDist < current.SqrDistance ||
                                    (sqrDist == current.SqrDistance &&
                                     collectorId.Value < current.CollectorSyncId.Value))
                                {
                                    state.Best[dropId] = new BestCollector
                                    {
                                        CollectorSyncId = collectorId,
                                        SqrDistance = sqrDist,
                                        Position = collectorPos,
                                    };
                                }
                            }
                        }
                    }).Invoke(ref bestState);

            (Lookup<BestCollector> Best, Lookup<List<GoodsData>> CollectorsDrops, List<Entity> RemoveDrops) applyState =
                (_bestCollectors, _collectorsDrops, _removeDrops);
            _world
                .ForEachEntity<(Lookup<BestCollector> Best, Lookup<List<GoodsData>> CollectorsDrops, List<Entity>
                    RemoveDrops), SyncId, GoodsId, GoodsAmount, Position, MaxSpeed>(in dropsDesc, ref applyState,
                    static (
                        ref (Lookup<BestCollector> Best, Lookup<List<GoodsData>> CollectorsDrops, List<Entity>
                            RemoveDrops) state, EntityRef entity, in SyncId dropId, in GoodsId goodsId,
                        in GoodsAmount goodsAmount, ref Position position, in MaxSpeed maxSpeed) =>
                    {
                        if (!state.Best.TryGetValue(dropId.Value, out var best))
                        {
                            return;
                        }

                        position = fix3.MoveTowards(position, best.Position, maxSpeed);
                        if (fix3.SqrDistance(position, best.Position) >= fix.One / 10)
                        {
                            return;
                        }

                        state.RemoveDrops.Add(entity);
                        if (!state.CollectorsDrops.TryGetValue(best.CollectorSyncId.Value, out var collected))
                        {
                            state.CollectorsDrops[best.CollectorSyncId.Value] = collected = new List<GoodsData>();
                        }

                        collected.Add(new() { GoodsId = goodsId, GoodsAmount = goodsAmount });
                    }).Invoke(ref applyState);

            var collectorsDrops = _collectorsDrops;
            _world.ForEach<Lookup<List<GoodsData>>, SyncId, GoodsDrop>(in collectorsDesc, ref collectorsDrops,
                static (ref Lookup<List<GoodsData>> collectedDrops, in SyncId collectorId, ref GoodsDrop drop) =>
                {
                    if (!collectedDrops.TryGetValue(collectorId.Value, out var toCollect))
                    {
                        return;
                    }

                    var values = drop.Values.ToBuilder();
                    foreach (var item in toCollect)
                    {
                        if (!values.ContainsKey(item.GoodsId))
                        {
                            values[item.GoodsId] = 0;
                        }

                        values[item.GoodsId] += item.GoodsAmount;
                    }

                    drop = new() { Values = values.ToImmutable() };
                }).Invoke(ref collectorsDrops);

            foreach (var item in _removeDrops)
            {
                _history.MarkForDisposal(item, tick);
                _world.Remove<Alive>(item);
            }
        }

        private static int2 GetQuantizedSquare(fix2 position)
        {
            return new int2(
                (int)position.x / SquareSize,
                (int)position.y / SquareSize);
        }

        internal struct DropRef
        {
            public int SyncId;
            public fix2 PositionXZ;
        }

        internal struct GoodsData
        {
            public GoodsAmount GoodsAmount;
            public GoodsId GoodsId;
        }

        internal struct BestCollector
        {
            public fix SqrDistance;
            public SyncId CollectorSyncId;
            public Position Position;
        }
    }
}
