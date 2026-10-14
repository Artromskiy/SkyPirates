using Delta;
using Delta.ECS;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.Components.Config;
using SkyPirates.Shared.Components.Framed;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.IServices;
using SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace SkyPirates.Shared.Systems
{
    public class SetMultiTargetSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, Targets, TargetSearchDistance, TargetSearchPosition, TeamId>().
            Alive().NotDisabled();

        private readonly World _world;
        private readonly ITargetSearchSystem _targetSearch;

        private readonly List<Entity> _targetsCache = new();

        public SetMultiTargetSystem(World world, ITargetSearchSystem targetSearch)
        {
            _world = world;
            _targetSearch = targetSearch;
        }

        public void Tick(int tick, fix deltaTime)
        {
            (ITargetSearchSystem TargetSearch, List<Entity> TargetsCache) state = (_targetSearch, _targetsCache);
            var desc = _desc;
            _world.ForEach<(ITargetSearchSystem TargetSearch, List<Entity> TargetsCache), TargetSearchDistance, TargetSearchPosition, Targets, TeamId>(in desc, ref state,
                static (ref (ITargetSearchSystem TargetSearch, List<Entity> TargetsCache) state, in TargetSearchDistance searchDistance, in TargetSearchPosition searchPosition, ref Targets target, in TeamId team) =>
                {
                    state.TargetsCache.Clear();
                    state.TargetSearch.FindTargets(in searchDistance, in searchPosition, in team, state.TargetsCache);
                    if (state.TargetsCache.Count > 0)
                    {
                        target.Entities = new(state.TargetsCache);
                    }
                }).Invoke(ref state);
        }
    }
}
