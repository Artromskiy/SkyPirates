using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace SlyPirates.Shared.Systems
{
    public class SetMultiTargetSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, Targets, TargetSearchDistance, TargetSearchPosition, TeamId>().
            Alive().NotDisabled();

        private readonly World _world;
        private readonly ITargetSearchSystem _targetSearch;

        private readonly List<SyncId> _targetsCache = new();

        public SetMultiTargetSystem(World world, ITargetSearchSystem targetSearch)
        {
            _world = world;
            _targetSearch = targetSearch;
        }

        public void Tick(int tick, fix deltaTime)
        {
            (ITargetSearchSystem TargetSearch, List<SyncId> TargetsCache) state = (_targetSearch, _targetsCache);
            var desc = _desc;
            _world.ForEach<(ITargetSearchSystem TargetSearch, List<SyncId> TargetsCache), TargetSearchDistance, TargetSearchPosition, Targets, TeamId>(in desc, ref state,
                static (ref (ITargetSearchSystem TargetSearch, List<SyncId> TargetsCache) state, in TargetSearchDistance searchDistance, in TargetSearchPosition searchPosition, ref Targets target, in TeamId team) =>
                {
                    state.TargetsCache.Clear();
                    state.TargetSearch.FindTargets(in searchDistance, in searchPosition, in team, state.TargetsCache);
                    target.SyncIds ??= new();
                    target.SyncIds.Clear();
                    target.SyncIds.AddRange(state.TargetsCache);
                }).Invoke(ref state);
        }
    }
}
