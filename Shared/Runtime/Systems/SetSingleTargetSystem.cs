using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Shared.Systems
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
            var targetSearch = _targetSearch;
            var desc = _desc;
            _world.ForEach<ITargetSearchSystem, Position, TargetSearchDistance, TargetSearchPosition, Target, TeamId>(in desc, ref targetSearch,
                static (ref ITargetSearchSystem targetSearch, in Position position, in TargetSearchDistance searchDistance, in TargetSearchPosition searchPosition, ref Target target, in TeamId team) =>
                target.Entity = targetSearch.FindTarget(in position, in searchDistance, in searchPosition, in team)).Invoke(ref targetSearch);
        }
    }
}
