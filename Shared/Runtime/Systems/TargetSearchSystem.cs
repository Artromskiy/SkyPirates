using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core.Collections;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.IServices;
using System.Collections.Generic;

namespace SlyPirates.Shared.Systems
{
    /// <summary>
    /// Caches target entities and their positions quantized by <see cref="SquareSize"/>.
    /// Use for fast nearest search.
    /// </summary>
    public sealed class
        TargetSearchSystem : ITargetSearchSystem // Should be used before any Position/Team writes for accurate search
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.WhereAll<RecivedDamage, Position, TeamId, SyncId>().Alive().NotDisabled();

        private const int SquareSize = 5;

        private readonly World _world;

        // teamId -> quad -> cached target position and entity
        private readonly Dictionary<int, Lookup2D<List<TargetEntry>>> _targets = new();
        private readonly List<TargetEntry> _targetsCache = new();
        private readonly List<List<TargetEntry>> _targetListPool = new();
        private (Dictionary<int, Lookup2D<List<TargetEntry>>> Targets, List<List<TargetEntry>> Lists, int NextList) _buildState;
        private readonly IOperation<(Dictionary<int, Lookup2D<List<TargetEntry>>> Targets, List<List<TargetEntry>> Lists, int NextList)> _buildTargets;

        public TargetSearchSystem(World world)
        {
            _world = world;
            _buildState = (_targets, _targetListPool, 0);
            var desc = _desc;
            _buildTargets = world.ForEachEntity<
                (Dictionary<int, Lookup2D<List<TargetEntry>>> Targets, List<List<TargetEntry>> Lists, int NextList),
                Position, TeamId, SyncId>(in desc, ref _buildState,
                static (ref (Dictionary<int, Lookup2D<List<TargetEntry>>> Targets, List<List<TargetEntry>> Lists, int NextList) state,
                    EntityRef entity, in Position position, in TeamId teamId, in SyncId syncId) =>
                {
                    var quad = GetQuantizedSquare(position.Value.xz);
                    if (!state.Targets.TryGetValue(teamId, out var team))
                    {
                        state.Targets[teamId] = team = new();
                    }

                    if (!team.TryGetValue(quad.x, quad.y, out var list))
                    {
                        if (state.NextList == state.Lists.Count)
                        {
                            list = new List<TargetEntry>(8);
                            state.Lists.Add(list);
                        }
                        else
                        {
                            list = state.Lists[state.NextList];
                        }
                        state.NextList++;
                        team[quad.x, quad.y] = list;
                    }

                    list.Add(new TargetEntry(entity, syncId, position.Value));
                });
        }

        public Entity? FindTarget(
            in Position position,
            in TargetSearchDistance searchDistance,
            in TargetSearchPosition searchPosition,
            in TeamId team)
        {
            _targetsCache.Clear();
            FindEntityTargets(in searchDistance, in searchPosition, in team, _targetsCache);

            var origin = (fix3)position;

            Entity? best = null;
            fix bestDist = fix.MaxValue;
            int bestSyncId = int.MaxValue;

            foreach (var target in _targetsCache)
            {
                var dist = fix3.SqrDistance(target.Position, origin);

                if (best == null || dist < bestDist)
                {
                    best = target.Entity;
                    bestDist = dist;
                    bestSyncId = target.SyncId.Value;
                }
                else if (dist == bestDist)
                {
                    int syncId = target.SyncId.Value;
                    if (syncId < bestSyncId)
                    {
                        best = target.Entity;
                        bestSyncId = syncId;
                    }
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
            {
                targets.Add(_targetsCache[i].SyncId);
            }
        }

        private void FindEntityTargets(
            in TargetSearchDistance searchDistance,
            in TargetSearchPosition searchPosition,
            in TeamId team,
            List<TargetEntry> targets)
        {
            var center = (fix3)searchPosition;
            fix distance = searchDistance;

            var range = new fix2(distance, distance);
            var min = GetQuantizedSquare(center.xz - range);
            var max = GetQuantizedSquare(center.xz + range);
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
                            var target = list[i];
                            if (fix3.SqrDistance(target.Position, center) <
                                sqrSearchDistance)
                            {
                                targets.Add(target);
                            }
                        }
                    }
                }
            }
        }

        public void Tick(int tick, fix deltaTime)
        {
            for (int i = 0; i < _buildState.NextList; i++)
            {
                _targetListPool[i].Clear();
            }

            foreach (var team in _targets.Values)
            {
                team.Clear();
            }

            _buildState.NextList = 0;
            _buildTargets.Invoke(ref _buildState);
        }

        private readonly struct TargetEntry
        {
            public readonly Entity Entity;
            public readonly SyncId SyncId;
            public readonly fix3 Position;

            public TargetEntry(Entity entity, SyncId syncId, fix3 position)
            {
                Entity = entity;
                SyncId = syncId;
                Position = position;
            }
        }

        private static int2 GetQuantizedSquare(fix2 position)
        {
            return new int2(
                (int)position.x / SquareSize,
                (int)position.y / SquareSize);
        }
    }
}
