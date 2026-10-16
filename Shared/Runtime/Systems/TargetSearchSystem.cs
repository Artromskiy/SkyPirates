using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Core.Collections;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.IServices;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Systems
{
    /// <summary>
    /// Caches Entities with their position quantized by <see cref="SquareSize"/>.
    /// Use for fast nearest search
    /// </summary>
    public sealed class
        TargetSearchSystem : ITargetSearchSystem // Should be used before any Position/Team writes for accurate search
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.WhereAll<RecivedDamage, Position, TeamId>().Alive().NotDisabled();

        private const int SquareSize = 5;

        private readonly World _world;

        // teamId -> quad -> entities
        private readonly Dictionary<int, Lookup2D<List<Entity>>> _targets = new();
        private readonly List<Entity> _targetsCache = new();

        private readonly Lookup _entitiesLookup = new();

        public TargetSearchSystem(World world)
        {
            _world = world;
        }

        public Entity? FindTarget(
            in Position position,
            in TargetSearchDistance searchDistance,
            in TargetSearchPosition searchPosition,
            in TeamId team)
        {
            _targetsCache.Clear();
            FindEntityTargets(in searchDistance, in searchPosition, in team, _targetsCache);

            var origin = ((fix3)position).xz;

            Entity? best = null;
            fix bestDist = fix.MaxValue;
            int bestSyncId = int.MaxValue;

            foreach (var entity in _targetsCache)
            {
                var targetPosXZ = ((fix3)_world.Get<Position>(entity)).xz;
                int syncId = _world.Get<SyncId>(entity).Value;
                var dist = fix2.SqrDistance(targetPosXZ, origin);

                if (best == null ||
                    dist < bestDist ||
                    (dist == bestDist && syncId < bestSyncId))
                {
                    best = entity;
                    bestDist = dist;
                    bestSyncId = syncId;
                }
            }

            return best;
        }

        public void FindTargets(
            in TargetSearchDistance searchDistance,
            in TargetSearchPosition searchPosition,
            in TeamId team,
            List<SyncId> targets)
        {
            _targetsCache.Clear();
            FindEntityTargets(in searchDistance, in searchPosition, in team, _targetsCache);

            for (int i = 0; i < _targetsCache.Count; i++)
                targets.Add(_world.Get<SyncId>(_targetsCache[i]));
        }

        private void FindEntityTargets(
            in TargetSearchDistance searchDistance,
            in TargetSearchPosition searchPosition,
            in TeamId team,
            List<Entity> targets)
        {
            _entitiesLookup.Clear();
            var center = ((fix3)searchPosition).xz;
            fix distance = searchDistance;

            var range = new fix2(distance, distance);
            var min = GetQuantizedSquare(center - range);
            var max = GetQuantizedSquare(center + range);
            var searchPositionXZ = center;
            var sqrSearchDistance = distance * distance;

            foreach (var kv in _targets)
            {
                if (kv.Key == team)
                {
                    continue;
                }

                var grid = kv.Value;

                for (int y = min.y; y <= max.y; y++)
                {
                    for (int x = min.x; x <= max.x; x++)
                    {
                        if (!grid.TryGetValue(x, y, out var list))
                        {
                            continue;
                        }

                        for (int i = 0; i < list.Count; i++)
                        {
                            if (_entitiesLookup.Has(list[i].Index))
                            {
                                continue;
                            }

                            if (fix2.SqrDistance(((fix3)_world.Get<Position>(list[i])).xz, searchPositionXZ) <
                                sqrSearchDistance)
                            {
                                targets.Add(list[i]);
                                _entitiesLookup.Add(list[i].Index);
                            }
                        }
                    }
                }
            }
        }

        public void Tick(int tick, fix deltaTime)
        {
            foreach (var team in _targets.Values)
            {
                team.Clear();
            }

            var targets = _targets;
            var desc = _desc;
            _world.ForEachEntity<Dictionary<int, Lookup2D<List<Entity>>>, Position, TeamId>(in desc, ref targets,
                static (ref Dictionary<int, Lookup2D<List<Entity>>> targets, EntityRef entity, in Position position,
                    in TeamId teamId) =>
                {
                    var quad = GetQuantizedSquare(position.Value.xz);

                    if (!targets.TryGetValue(teamId, out var team))
                    {
                        targets[teamId] = team = new();
                    }

                    if (!team.TryGetValue(quad.x, quad.y, out var list))
                    {
                        team[quad.x, quad.y] = list = new List<Entity>(8);
                    }

                    list.Add(entity);
                }).Invoke(ref targets);
        }

        private static int2 GetQuantizedSquare(fix2 position)
        {
            return new int2(
                (int)position.x / SquareSize,
                (int)position.y / SquareSize);
        }
    }
}
