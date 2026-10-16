using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    /// <summary>
    /// Sets value to <see href="Target"/> if it matches <see href="TargetSearchData"/> and <see href="TeamId"/> conditions
    /// </summary>
    public sealed class SetSingleTargetSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, TargetSearchDistance, TargetSearchPosition, Target, TeamId>().Alive().NotDisabled();

        private readonly World _world;
        private readonly ITargetSearchSystem _targetSearch;
        public SetSingleTargetSystem(World world, ITargetSearchSystem targetSearch)
        {
            _world = world;
            _targetSearch = targetSearch;
        }

        public void Tick(int tick, fix deltaTime)
        {
            (ITargetSearchSystem TargetSearch, World World) targetSearch = (_targetSearch, _world);
            var desc = _desc;
            _world.ForEach<(ITargetSearchSystem TargetSearch, World World), Position, TargetSearchDistance, TargetSearchPosition, Target, TeamId>(in desc, ref targetSearch,
                static (ref (ITargetSearchSystem TargetSearch, World World) targetSearch, in Position position, in TargetSearchDistance searchDistance, in TargetSearchPosition searchPosition, ref Target target, in TeamId team) =>
                {
                    Entity? found = targetSearch.TargetSearch.FindTarget(in position, in searchDistance, in searchPosition, in team);
                    target.SyncId = found.HasValue ? targetSearch.World.Get<SyncId>(found.Value) : null;
                }).Invoke(ref targetSearch);
        }
    }
}
