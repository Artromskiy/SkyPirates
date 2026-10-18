using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core.Collections;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SlyPirates.Shared.Systems
{
    // another heat death of universe system
    public class SquadGoodsDistributionSystem : IDeltaTickableExecutor
    {
        private readonly World _world;
        private readonly Dictionary<int, Dictionary<GoodsId, int>> _goodsPerSquad = new();
        private readonly Dictionary<int, SortedList<GoodsId, int>> _goodsPerUnit = new();
        private readonly Dictionary<int, List<SyncId>> _unitsPerSquad = new();

        private Query? _unitsDescCache;
        private Query _unitsDesc => _unitsDescCache ??= _world.
            WhereAll<SquadMember, SyncId, GoodsDrop>().Alive().NotDisabled();
        private readonly OrderedQuery _orderedUnitsDesc;

        private Query? _squadDescCache;
        private Query _squadDesc => _squadDescCache ??= _world.
            WhereAll<Squad, SyncId, GoodsDrop>().Alive().NotDisabled();

        public SquadGoodsDistributionSystem(World world)
        {
            _world = world;
            var units = _unitsDesc;
            var squadIdComparer = default(SquadIdComparer);
            var syncIdComparer = default(SyncIdComparer);
            _orderedUnitsDesc = units
                .OrderBy(ref squadIdComparer)
                .ThenBy(ref syncIdComparer);
        }

        public void Tick(int tick, fix deltaTime)
        {
            foreach (var item in _unitsPerSquad)
            {
                item.Value.Clear();
            }

            foreach (var item in _goodsPerSquad)
            {
                item.Value.Clear();
            }

            foreach (var item in _goodsPerUnit)
            {
                item.Value.Clear();
            }

            (Dictionary<int, Dictionary<GoodsId, int>> GoodsPerSquad, Dictionary<int, List<SyncId>> UnitsPerSquad) collectState = (_goodsPerSquad, _unitsPerSquad);
            _orderedUnitsDesc.ForEach(ref collectState,
                static (ref (Dictionary<int, Dictionary<GoodsId, int>> GoodsPerSquad, Dictionary<int, List<SyncId>> UnitsPerSquad) state, in SquadMember member, in GoodsDrop drop, in SyncId syncId) =>
                {
                    if (drop.Values != null)
                    {
                        if (!state.GoodsPerSquad.TryGetValue(member.SquadId.Value, out var squadGoods))
                        {
                            state.GoodsPerSquad[member.SquadId.Value] = squadGoods = new();
                        }

                        foreach (var item in drop.Values)
                        {
                            if (item.Value <= 0)
                            {
                                continue;
                            }

                            if (!squadGoods.TryAdd(item.Key, item.Value))
                            {
                                squadGoods[item.Key] += item.Value;
                            }
                        }
                    }

                    if (!state.UnitsPerSquad.TryGetValue(member.SquadId.Value, out var units))
                    {
                        state.UnitsPerSquad[member.SquadId.Value] = units = new();
                    }

                    units.Add(syncId);
                }).Invoke(ref collectState);
            // Keep squad goods until an active member can receive them.
            var goodsPerSquad = (_goodsPerSquad, _unitsPerSquad);
            var squadDesc = _squadDesc;
            _world.ForEach<(Dictionary<int, Dictionary<GoodsId, int>> GoodsPerSquad, Dictionary<int, List<SyncId>> UnitsPerSquad), GoodsDrop, SyncId>(in squadDesc, ref goodsPerSquad,
                static (ref (Dictionary<int, Dictionary<GoodsId, int>> GoodsPerSquad, Dictionary<int, List<SyncId>> UnitsPerSquad) state, ref GoodsDrop goods, in SyncId syncId) =>
                {
                    if (!state.UnitsPerSquad.TryGetValue(syncId.Value, out var units) || units.Count == 0)
                    {
                        return;
                    }

                    if (!state.GoodsPerSquad.TryGetValue(syncId.Value, out var squadGoods))
                    {
                        state.GoodsPerSquad[syncId.Value] = squadGoods = new();
                    }

                    foreach (var item in goods.Values)
                    {
                        if (item.Value <= 0)
                        {
                            continue;
                        }

                        if (!squadGoods.TryAdd(item.Key, item.Value))
                        {
                            squadGoods[item.Key] += item.Value;
                        }
                    }
                    goods = new() { Values = ImmutableSortedDictionary<GoodsId, int>.Empty };
                }).Invoke(ref goodsPerSquad);

            foreach ((int squadId, var units) in _unitsPerSquad)
            {
                if (!_goodsPerSquad.TryGetValue(squadId, out var squadGoods))
                {
                    continue;
                }

                int membersCount = units.Count;
                if (membersCount == 0)
                {
                    continue;
                }

                foreach (var goodsId in squadGoods.Keys.OrderBy(k => k))
                {
                    int total = squadGoods[goodsId];

                    int baseAmount = total / membersCount;
                    int remainder = total % membersCount;

                    for (int i = 0; i < membersCount; i++)
                    {
                        int amount = baseAmount + (i < remainder ? 1 : 0);
                        if (amount <= 0)
                        {
                            continue;
                        }

                        var syncId = units[i];

                        if (!_goodsPerUnit.TryGetValue(syncId, out var unitGoods))
                        {
                            _goodsPerUnit[syncId] = unitGoods = new();
                        }

                        if (!unitGoods.ContainsKey(goodsId))
                        {
                            unitGoods[goodsId] = 0;
                        }

                        unitGoods[goodsId] += amount;
                    }
                }
            }

            var goodsPerUnit = _goodsPerUnit;
            var unitsDesc = _unitsDesc;
            _world.ForEach<Dictionary<int, SortedList<GoodsId, int>>, SyncId, GoodsDrop>(in unitsDesc, ref goodsPerUnit,
                static (ref Dictionary<int, SortedList<GoodsId, int>> goodsPerUnit, in SyncId syncId, ref GoodsDrop drop) =>
                {
                    if (!goodsPerUnit.TryGetValue(syncId, out var distributedDrop) || distributedDrop.Count == 0)
                    {
                        drop = new() { Values = ImmutableSortedDictionary<GoodsId, int>.Empty };
                        return;
                    }

                    if (drop.Values?.SequenceEqual(distributedDrop, KeyValuePairComparer<GoodsId, int>.Default) ?? false)
                    {
                        return;
                    }

                    drop = new() { Values = distributedDrop.ToImmutableSortedDictionary() };
                }).Invoke(ref goodsPerUnit);
        }
    }
}
