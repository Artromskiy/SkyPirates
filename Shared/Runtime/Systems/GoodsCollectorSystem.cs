using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core.Collections;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;

namespace SlyPirates.Shared.Systems
{
    public sealed class GoodsCollectorSystem : IDeltaTickableExecutor
    {
        private const int SquareSize = 5;


        private readonly Lookup2D<List<DropRef>> _partitioning = new();
        private readonly Lookup<BestCollector> _bestCollectors = new();
        private readonly Lookup<List<GoodsData>> _collectorsDrops = new();

        private readonly List<Entity> _removeDrops = new();
        private readonly List<List<DropRef>> _dropListPool = new();
        private readonly List<List<GoodsData>> _collectorDropListPool = new();
        private Entity[] _entityBatch = System.Array.Empty<Entity>();

        private (Lookup2D<List<DropRef>> Grid, List<List<DropRef>> Lists, int NextList) _buildDropsState;
        private readonly IOperation<(Lookup2D<List<DropRef>> Grid, List<List<DropRef>> Lists, int NextList)> _buildDropGrid;
        private (Lookup2D<List<DropRef>> Grid, Lookup<BestCollector> Best, int BestCount) _findCollectorsState;
        private readonly IOperation<(Lookup2D<List<DropRef>> Grid, Lookup<BestCollector> Best, int BestCount)> _findCollectors;
        private (Lookup<BestCollector> Best, Lookup<List<GoodsData>> CollectorsDrops, List<Entity> RemoveDrops,
            List<List<GoodsData>> CollectorLists, int NextList) _moveDropsState;
        private readonly IOperation<(Lookup<BestCollector> Best, Lookup<List<GoodsData>> CollectorsDrops, List<Entity> RemoveDrops,
            List<List<GoodsData>> CollectorLists, int NextList)> _moveDrops;
        private Lookup<List<GoodsData>> _applyCollectedState;
        private readonly IOperation<Lookup<List<GoodsData>>> _applyCollectedGoods;

        private Query? _dropsDescCache;

        private Query _dropsDesc => _dropsDescCache ??= _world
            .WhereAll<GoodsId, SyncId, Position, GoodsAmount, MaxSpeed>().WhereNone<FlyDestination>().Alive()
            .NotDisabled();

        private Query? _collectorsDescCache;

        private Query _collectorsDesc => _collectorsDescCache ??=
            _world.WhereAll<SyncId, Position, GoodsDrop, GoodsCollectorRadius>().Alive().NotDisabled();

        private Query? _goodsOwnersDescCache;
        private Query _goodsOwnersDesc => _goodsOwnersDescCache ??= _world.WhereAll<SyncId, GoodsDrop>().Alive();

        private readonly World _world;

        public GoodsCollectorSystem(World world)
        {
            _world = world;
            _buildDropsState = (_partitioning, _dropListPool, 0);
            var dropsDesc = _dropsDesc;
            _buildDropGrid = world.ForEach<
                (Lookup2D<List<DropRef>> Grid, List<List<DropRef>> Lists, int NextList), SyncId, Position>(in dropsDesc,
                ref _buildDropsState,
                static (ref (Lookup2D<List<DropRef>> Grid, List<List<DropRef>> Lists, int NextList) state,
                    in SyncId syncId, in Position position) =>
                {
                    var posXZ = ((fix3)position).xz;
                    var quad = GetQuantizedSquare(posXZ);
                    if (!state.Grid.TryGetValue(quad.x, quad.y, out var list))
                    {
                        if (state.NextList == state.Lists.Count)
                        {
                            list = new List<DropRef>(8);
                            state.Lists.Add(list);
                        }
                        else
                        {
                            list = state.Lists[state.NextList];
                        }
                        state.NextList++;
                        state.Grid[quad.x, quad.y] = list;
                    }

                    list.Add(new DropRef { SyncId = syncId.Value, PositionXZ = posXZ });
                });

            _findCollectorsState = (_partitioning, _bestCollectors, 0);
            var collectorsDesc = _collectorsDesc;
            _findCollectors = world.ForEachEntity<
                (Lookup2D<List<DropRef>> Grid, Lookup<BestCollector> Best, int BestCount),
                SyncId, Position, GoodsCollectorRadius>(in collectorsDesc, ref _findCollectorsState,
                static (ref (Lookup2D<List<DropRef>> Grid, Lookup<BestCollector> Best, int BestCount) state,
                    EntityRef collector, in SyncId collectorId, in Position collectorPos, in GoodsCollectorRadius radius) =>
                {
                    SyncId ownerId = collector.TryGet(out SquadMember member)
                        ? member.SquadId
                        : collectorId;
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
                            bool hasCurrent = state.Best.TryGetValue(dropId, out var current);
                            if (!hasCurrent ||
                                sqrDist < current.SqrDistance ||
                                (sqrDist == current.SqrDistance && collectorId.Value < current.CollectorSyncId.Value))
                            {
                                if (!hasCurrent)
                                {
                                    state.BestCount++;
                                }

                                state.Best[dropId] = new BestCollector
                                {
                                    CollectorSyncId = collectorId,
                                    OwnerSyncId = ownerId,
                                    SqrDistance = sqrDist,
                                    Position = collectorPos,
                                };
                            }
                        }
                    }
                });

            _moveDropsState = (_bestCollectors, _collectorsDrops, _removeDrops, _collectorDropListPool, 0);
            _moveDrops = world.ForEachEntity<
                (Lookup<BestCollector> Best, Lookup<List<GoodsData>> CollectorsDrops, List<Entity> RemoveDrops,
                    List<List<GoodsData>> CollectorLists, int NextList),
                SyncId, GoodsId, GoodsAmount, Position, MaxSpeed>(in dropsDesc, ref _moveDropsState,
                static (ref (Lookup<BestCollector> Best, Lookup<List<GoodsData>> CollectorsDrops, List<Entity> RemoveDrops,
                    List<List<GoodsData>> CollectorLists, int NextList) state, EntityRef entity, in SyncId dropId,
                    in GoodsId goodsId, in GoodsAmount goodsAmount, ref Position position, in MaxSpeed maxSpeed) =>
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
                    if (!state.CollectorsDrops.TryGetValue(best.OwnerSyncId.Value, out var collected))
                    {
                        if (state.NextList == state.CollectorLists.Count)
                        {
                            collected = new List<GoodsData>();
                            state.CollectorLists.Add(collected);
                        }
                        else
                        {
                            collected = state.CollectorLists[state.NextList];
                        }
                        state.NextList++;
                        state.CollectorsDrops[best.OwnerSyncId.Value] = collected;
                    }

                    collected.Add(new() { GoodsId = goodsId, GoodsAmount = goodsAmount });
                });

            _applyCollectedState = _collectorsDrops;
            var goodsOwnersDesc = _goodsOwnersDesc;
            _applyCollectedGoods = world.ForEach<Lookup<List<GoodsData>>, SyncId, GoodsDrop>(in goodsOwnersDesc,
                ref _applyCollectedState,
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
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            for (int i = 0; i < _buildDropsState.NextList; i++)
            {
                _dropListPool[i].Clear();
            }
            for (int i = 0; i < _moveDropsState.NextList; i++)
            {
                _collectorDropListPool[i].Clear();
            }

            _partitioning.Clear();
            _bestCollectors.Clear();
            _collectorsDrops.Clear();
            _removeDrops.Clear();
            _findCollectorsState.BestCount = 0;

            _buildDropsState.NextList = 0;
            _buildDropGrid.Invoke(ref _buildDropsState);

            if (_buildDropsState.NextList != 0)
            {
                _findCollectors.Invoke(ref _findCollectorsState);
            }

            _moveDropsState.NextList = 0;
            if (_findCollectorsState.BestCount != 0)
            {
                _moveDrops.Invoke(ref _moveDropsState);
            }

            if (_moveDropsState.NextList != 0)
            {
                _applyCollectedGoods.Invoke(ref _applyCollectedState);
            }

            if (_removeDrops.Count != 0)
            {
                ReadOnlySpan<Entity> batch = Batch(_removeDrops);
                _world.Add<PendingDestroy>(batch);
                _world.Remove<Alive>(batch);
            }
        }

        private ReadOnlySpan<Entity> Batch(List<Entity> entities)
        {
            if (_entityBatch.Length < entities.Count)
            {
                System.Array.Resize(ref _entityBatch, System.Math.Max(entities.Count, _entityBatch.Length * 2));
            }

            entities.CopyTo(_entityBatch);
            return _entityBatch.AsSpan(0, entities.Count);
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
            public SyncId OwnerSyncId;
            public Position Position;
        }
    }
}
