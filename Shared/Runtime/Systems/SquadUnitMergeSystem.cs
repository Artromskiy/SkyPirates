using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace SlyPirates.Shared.Systems
{
    public sealed class SquadUnitMergeSystem : IDeltaTickableExecutor
    {
        private static readonly fix Three = fix.One + fix.One + fix.One;

        private readonly World _world;
        private readonly Dictionary<(int SquadId, UnitId UnitId, int Level), List<Unit>> _unitsPerGroup = new();

        private Query? _unitsQueryCache;
        private Query _unitsQuery => _unitsQueryCache ??= _world.
            WhereAll<SquadMember, UnitId, SyncId, Health, MaxHealth, Damage>().Alive();

        public SquadUnitMergeSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            foreach (var units in _unitsPerGroup.Values)
            {
                units.Clear();
            }

            var state = (_world, _unitsPerGroup);
            var query = _unitsQuery;
            _world.ForEachEntity<(World World, Dictionary<(int SquadId, UnitId UnitId, int Level), List<Unit>> UnitsPerGroup), SquadMember, UnitId, SyncId, Health, MaxHealth, Damage>(
                in query,
                ref state,
                static (ref (World World, Dictionary<(int SquadId, UnitId UnitId, int Level), List<Unit>> UnitsPerGroup) state,
                    EntityRef entity,
                    in SquadMember member,
                    in UnitId unitId,
                    in SyncId syncId,
                    in Health health,
                    in MaxHealth maxHealth,
                    in Damage damage) =>
                {
                    var level = state.World.Has<Level>(entity)
                        ? state.World.Get<Level>(entity).Value
                        : 1;
                    var groupId = (member.SquadId.Value, unitId, level);
                    if (!state.UnitsPerGroup.TryGetValue(groupId, out var units))
                    {
                        state.UnitsPerGroup.Add(groupId, units = new List<Unit>());
                    }

                    units.Add(new Unit(entity, syncId.Value, level, maxHealth.Value, damage.Value));
                }).Invoke(ref state);

            foreach (var units in _unitsPerGroup.Values)
            {
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
                    strongest = Merge(strongest, first, second, third, tick);

                    units.RemoveRange(0, 3);
                    units.Add(strongest);
                    units.Sort(static (left, right) => left.SyncId.CompareTo(right.SyncId));
                }
            }
        }

        private Unit Merge(Unit strongest, Unit first, Unit second, Unit third, int tick)
        {
            ref var level = ref _world.GetOrAdd<Level>(strongest.Entity);
            level.Value = strongest.Level + 1;

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
                _world.Add<PendingDestroy>(first.Entity);
                _world.Remove<Alive>(first.Entity);
            }
            if (second.SyncId != strongest.SyncId)
            {
                _world.Add<PendingDestroy>(second.Entity);
                _world.Remove<Alive>(second.Entity);
            }
            if (third.SyncId != strongest.SyncId)
            {
                _world.Add<PendingDestroy>(third.Entity);
                _world.Remove<Alive>(third.Entity);
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

        private readonly struct Unit
        {
            public readonly Entity Entity;
            public readonly int SyncId;
            public readonly int Level;
            public readonly fix MaxHealth;
            public readonly fix Damage;

            public Unit(Entity entity, int syncId, int level, fix maxHealth, fix damage)
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
