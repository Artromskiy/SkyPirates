using System;
using System.Collections.Generic;
using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public sealed class SquadUnitMergeSystem : IDeltaTickableExecutor
    {
        private static readonly fix Three = fix.One + fix.One + fix.One;

        private readonly World _world;
        private readonly Dictionary<(int SquadId, UnitId UnitId, int Level), List<Unit>> _unitsPerGroup = new();
        private readonly List<Entity> _destroy = new();
        private Entity[] _entityBatch = System.Array.Empty<Entity>();
        private Dictionary<(int SquadId, UnitId UnitId, int Level), List<Unit>> _collectState;
        private readonly IOperation<Dictionary<(int SquadId, UnitId UnitId, int Level), List<Unit>>> _collectUnits;

        private Query? _unitsQueryCache;
        private Query _unitsQuery => _unitsQueryCache ??= _world.
            WhereAll<SquadMember, UnitId, SyncId, Health, MaxHealth, Damage>().
            WhereNone<PendingDestroy>().Alive();

        public SquadUnitMergeSystem(World world)
        {
            _world = world;
            _collectState = _unitsPerGroup;
            var query = _unitsQuery;
            _collectUnits = world.ForEachEntity<Dictionary<(int SquadId, UnitId UnitId, int Level), List<Unit>>,
                SquadMember, UnitId, SyncId, Health, MaxHealth, Damage>(in query, ref _collectState,
                static (ref Dictionary<(int SquadId, UnitId UnitId, int Level), List<Unit>> unitsPerGroup,
                    EntityRef entity, in SquadMember member, in UnitId unitId, in SyncId syncId, in Health health,
                    in MaxHealth maxHealth, in Damage damage) =>
                {
                    int level = entity.TryGet(out Level unitLevel) ? unitLevel.Value : 1;
                    var groupId = (member.SquadId.Value, unitId, level);
                    if (!unitsPerGroup.TryGetValue(groupId, out var units))
                    {
                        unitsPerGroup.Add(groupId, units = new List<Unit>());
                    }

                    units.Add(new Unit(entity, syncId.Value, level, maxHealth.Value, damage.Value));
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            foreach (var units in _unitsPerGroup.Values)
            {
                units.Clear();
            }
            _destroy.Clear();

            _collectUnits.Invoke(ref _collectState);

            foreach (var units in _unitsPerGroup.Values)
            {
                if (units.Count < 3)
                {
                    continue;
                }

                units.Sort(static (left, right) => left.SyncId.CompareTo(right.SyncId));
                while (units.Count >= 3)
                {
                    var strongest = units[0];
                    if (IsStronger(units[1], strongest))
                    {
                        strongest = units[1];
                    }

                    if (IsStronger(units[2], strongest))
                    {
                        strongest = units[2];
                    }

                    var first = units[0];
                    var second = units[1];
                    var third = units[2];
                    strongest = Merge(strongest, first, second, third);

                    units.RemoveRange(0, 3);
                    units.Add(strongest);
                    units.Sort(static (left, right) => left.SyncId.CompareTo(right.SyncId));
                }
            }

            if (_destroy.Count != 0)
            {
                ReadOnlySpan<Entity> batch = Batch(_destroy);
                _world.Add<PendingDestroy>(batch);
                _world.Remove<Alive>(batch);
            }
        }

        private Unit Merge(Unit strongest, Unit first, Unit second, Unit third)
        {
            Level level = strongest.Level + 1;
            _world.AddOrSet(strongest.Entity, level);

            ref var health = ref _world.GetRef<Health>(strongest.Entity);
            health.Value = TripleStat(health.Value);
            ref var maxHealth = ref _world.GetRef<MaxHealth>(strongest.Entity);
            maxHealth.Value = TripleStat(maxHealth.Value);
            ref var damage = ref _world.GetRef<Damage>(strongest.Entity);
            damage.Value = TripleStat(damage.Value);

            var merged = new Unit(
                strongest.Entity,
                strongest.SyncId,
                level.Value,
                maxHealth.Value,
                damage.Value);

            if (first.SyncId != strongest.SyncId)
            {
                _destroy.Add(first.Entity);
            }
            if (second.SyncId != strongest.SyncId)
            {
                _destroy.Add(second.Entity);
            }
            if (third.SyncId != strongest.SyncId)
            {
                _destroy.Add(third.Entity);
            }

            return merged;
        }

        private static bool IsStronger(Unit candidate, Unit current)
        {
            if (candidate.Level != current.Level)
            {
                return candidate.Level > current.Level;
            }

            var candidatePower = (long)candidate.MaxHealth.raw + candidate.Damage.raw;
            var currentPower = (long)current.MaxHealth.raw + current.Damage.raw;
            if (candidatePower != currentPower)
            {
                return candidatePower > currentPower;
            }

            return candidate.SyncId < current.SyncId;
        }

        private static fix TripleStat(fix value)
        {
            if (value.raw > fix.MaxValue.raw / 3)
            {
                return fix.MaxValue;
            }

            if (value.raw < fix.MinValue.raw / 3)
            {
                return fix.MinValue;
            }

            return value * Three;
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

        private readonly struct Unit
        {
            public readonly Entity Entity;
            public readonly int SyncId;
            public readonly int Level;
            public readonly fix MaxHealth;
            public readonly fix Damage;

            public Unit(
                Entity entity,
                int syncId,
                int level,
                fix maxHealth,
                fix damage)
            {
                Entity = entity;
                SyncId = syncId;
                Level = level;
                MaxHealth = maxHealth;
                Damage = damage;
            }
        }
    }
}
