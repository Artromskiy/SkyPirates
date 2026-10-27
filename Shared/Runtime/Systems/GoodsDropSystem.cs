using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.IFactories;
using SlyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Delta.Diagnostics;

namespace SlyPirates.Shared.Systems
{
    public class GoodsDropSystem : IDeltaTickableExecutor
    {
        private static readonly fix DropRange = fix.One * 5 / 2;
        private Query? _createDescCache;
        private Query _createDesc => _createDescCache ??= _world.WhereAll<Health, Position, SyncId, SyncIdReserve, RandomSeed>().Alive().NotDisabled();

        private readonly World _world;
        private readonly IConfigedEntityFactory<GoodsId> _configedEntityFactory;
        private readonly IResultOperation<Entity> _findMap;
        private Entity _mapEntity;

        private List<(Entity Entity, int SyncId)> _dead = new();
        private readonly List<DropInfo> _dropInfos = new();
        private readonly Dictionary<int, int> _remainingMembers = new();
        private readonly IOperation<List<(Entity Entity, int SyncId)>> _collectDead;

        public GoodsDropSystem(World world, IConfigedEntityFactory<GoodsId> configedEntityFactory)
        {
            _world = world;
            _configedEntityFactory = configedEntityFactory;
            _findMap = world.WhereAll<HexMap>().First();
            var desc = _createDesc;
            _collectDead = world.ForEachEntity<List<(Entity Entity, int SyncId)>, SyncId, Health>(in desc, ref _dead,
                static (ref List<(Entity Entity, int SyncId)> dead, EntityRef entity, in SyncId syncId, in Health health) =>
                {
                    if (health <= fix.Zero)
                    {
                        dead.Add(((Entity)entity, syncId.Value));
                    }
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _dead.Clear();
            _dropInfos.Clear();
            _remainingMembers.Clear();

            if (!_mapEntity.IsValid)
            {
                _mapEntity = _findMap.Invoke();
            }

            HexMap map = _mapEntity.IsValid ? _world.Get<HexMap>(_mapEntity) : default;

            _collectDead.Invoke(ref _dead);
            if (_dead.Count == 0)
            {
                return;
            }

            _dead.Sort(static (left, right) => left.SyncId.CompareTo(right.SyncId));
            foreach (var deadEntity in _dead)
            {
                Entity entity = deadEntity.Entity;
                var values = _world.TryGet(entity, out GoodsDrop entityGoods)
                    ? entityGoods.Values.ToBuilder()
                    : ImmutableSortedDictionary.CreateBuilder<GoodsId, int>();
                Entity squad = default;
                ImmutableSortedDictionary<GoodsId, int>.Builder? squadValues = null;
                bool tookSquadGoods = false;

                if (_world.TryGet(entity, out SquadMember squadMember))
                {
                    var squadId = squadMember.SquadId;
                    squad = _world.GetEntity(squadId);
                    if (!_remainingMembers.TryGetValue(squadId.Value, out int membersCount))
                    {
                        membersCount = _world.Get<SquadMemberCount>(squad).Value;
                    }

                    _remainingMembers[squadId.Value] = membersCount - 1;

                    var goods = _world.Get<GoodsDrop>(squad);
                    squadValues = goods.Values.ToBuilder();
                    foreach (var item in goods.Values)
                    {
                        if (item.Value <= 0)
                        {
                            continue;
                        }

                        tookSquadGoods = true;
                        int amount = item.Value / membersCount + (item.Value % membersCount == 0 ? 0 : 1);
                        int remaining = item.Value - amount;
                        if (remaining == 0)
                        {
                            squadValues.Remove(item.Key);
                        }
                        else
                        {
                            squadValues[item.Key] = remaining;
                        }

                        if (!values.TryGetValue(item.Key, out int personalAmount))
                        {
                            personalAmount = 0;
                        }

                        values[item.Key] = personalAmount + amount;
                    }
                }

                int typesCount = 0;
                foreach (var item in values)
                {
                    if (item.Value > 0)
                    {
                        typesCount++;
                    }
                }

                if (typesCount == 0)
                {
                    continue;
                }

                int remainingIds = _world.Get<SyncIdReserve>(entity).RemainingCount();
                Delta.Diagnostics.Debug.Assert(remainingIds >= typesCount);
                if (remainingIds < typesCount)
                {
                    continue;
                }

                if (tookSquadGoods)
                {
                    _world.GetRef<GoodsDrop>(squad) = new() { Values = squadValues!.ToImmutable() };
                }

                ref var syncIdReserve = ref _world.GetRef<SyncIdReserve>(entity);
                ref var seed = ref _world.GetRef<RandomSeed>(entity);
                var position = _world.Get<Position>(entity);
                int baseSlots = remainingIds / typesCount;
                int remainderSlots = remainingIds % typesCount;
                int typeIndex = 0;
                foreach (var item in values)
                {
                    if (item.Value <= 0)
                    {
                        continue;
                    }

                    int slots = baseSlots + (typeIndex < remainderSlots ? 1 : 0);
                    int baseAmount = item.Value / slots;
                    int remainderAmount = item.Value % slots;
                    for (int i = 0; i < slots; i++)
                    {
                        int amount = baseAmount + (i < remainderAmount ? 1 : 0);
                        if (amount == 0)
                        {
                            continue;
                        }

                        fix xOffset = seed.NextFixRange(-DropRange, DropRange);
                        fix zOffset = seed.NextFixRange(-DropRange, DropRange);
                        _dropInfos.Add(new DropInfo
                        {
                            Position = position,
                            EndPosition = FindDropPosition(map, position, xOffset, zOffset),
                            GoodsId = item.Key,
                            Amount = amount,
                            SyncId = syncIdReserve.GetNext(),
                            Rotation = seed.NextFixRange(0, 360)
                        });
                    }

                    typeIndex++;
                }
            }

            foreach (var item in _dropInfos)
            {
                EntityParameters parameters = new()
                {
                    SyncId = item.SyncId,
                    RandomSeed = default,
                    SyncIdReserve = default,
                };

                var drop = _configedEntityFactory.Create((item.GoodsId, parameters));
                _world.GetRef<Position>(drop) = item.Position;
                _world.GetRef<Rotation>(drop) = item.Rotation;
                _world.AddOrSet(drop, (GoodsAmount)item.Amount);

                _world.AddOrSet(drop, new FlyDestination
                {
                    StartPosition = item.Position,
                    EndPosition = item.EndPosition,
                });
            }
        }

        private static fix3 FindDropPosition(HexMap map, Position origin, fix xOffset, fix zOffset)
        {
            fix3 originPosition = origin;
            fix2 desiredPosition = originPosition.xz + new fix2(xOffset, zOffset);
            if (map.SurfaceElevations == null)
            {
                return new fix3(desiredPosition.x, originPosition.y, desiredPosition.y);
            }

            int2 landingColumn = Hex.WorldToAxial(desiredPosition);
            if (!map.SurfaceElevations.TryGetValue(landingColumn, out int landingElevation))
            {
                landingColumn = Hex.WorldToAxial(originPosition.xz);
                if (!map.SurfaceElevations.TryGetValue(landingColumn, out landingElevation))
                {
                    return originPosition;
                }
            }

            fix3 landingPosition = Hex.AxialToWorld(new int3(landingColumn.x, landingElevation, landingColumn.y));
            return FindLastFreeDropPosition(map, originPosition, landingPosition);
        }

        private static fix3 FindLastFreeDropPosition(HexMap map, fix3 start, fix3 landing)
        {
            if (map.Data == null || map.SurfaceElevations == null)
            {
                return landing;
            }

            int2 startColumn = Hex.WorldToAxial(start.xz);
            fix horizontalDistance = fix2.Distance(start.xz, landing.xz);
            fix verticalDistance = Maths.Abs(landing.y - start.y) + 8;
            fix sampleDistance = Hex.InnerRadius / 2;
            int sampleCount = Math.Max(1,
                (int)Maths.Ceil((horizontalDistance + verticalDistance) / sampleDistance));
            fix3 lastFreePosition = start;

            for (int i = 1; i < sampleCount; i++)
            {
                fix percent = (fix)i / sampleCount;
                fix3 pathPosition = GetDropFlightPosition(start, landing, percent);
                int3 coordinate = Hex.WorldToAxial(pathPosition);
                if (coordinate.xz != startColumn && map.Data.ContainsKey(coordinate))
                {
                    int2 lastFreeColumn = Hex.WorldToAxial(lastFreePosition.xz);
                    if (map.SurfaceElevations.TryGetValue(lastFreeColumn, out int elevation))
                    {
                        return Hex.AxialToWorld(new int3(lastFreeColumn.x, elevation, lastFreeColumn.y));
                    }

                    if (map.SurfaceElevations.TryGetValue(startColumn, out int startElevation))
                    {
                        return Hex.AxialToWorld(new int3(startColumn.x, startElevation, startColumn.y));
                    }

                    return landing;
                }

                lastFreePosition = pathPosition;
            }

            return landing;
        }

        private static fix3 GetDropFlightPosition(fix3 start, fix3 end, fix percent)
        {
            const int arcHeight = 4;
            fix2 xz = start.xz + (end.xz - start.xz) * percent;
            fix y = Maths.Lerp(start.y, end.y, percent) + 4 * percent * (1 - percent) * arcHeight;
            return new fix3(xz.x, y, xz.y);
        }

        private struct DropInfo
        {
            public Position Position;
            public fix3 EndPosition;
            public Rotation Rotation;
            public GoodsId GoodsId;
            public SyncId SyncId;
            public int Amount;
        }
    }
}
