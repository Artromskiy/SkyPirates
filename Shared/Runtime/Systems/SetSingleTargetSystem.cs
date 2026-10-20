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
        private (ITargetSearchSystem TargetSearch, World World) _state;
        private readonly IOperation<(ITargetSearchSystem TargetSearch, World World)> _setTargets;

        public SetSingleTargetSystem(World world, ITargetSearchSystem targetSearch)
        {
            _world = world;
            _state = (targetSearch, world);
            var desc = _desc;
            _setTargets = world.ForEach<(ITargetSearchSystem TargetSearch, World World), Position, TargetSearchDistance, TargetSearchPosition, Target, TeamId>(in desc, ref _state,
                static (ref (ITargetSearchSystem TargetSearch, World World) state, in Position position, in TargetSearchDistance searchDistance, in TargetSearchPosition searchPosition, ref Target target, in TeamId team) =>
                {
                    Entity? found = state.TargetSearch.FindTarget(in position, in searchDistance, in searchPosition, in team);
                    target.SyncId = found.HasValue ? state.World.Get<SyncId>(found.Value) : null;
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _setTargets.Invoke(ref _state);
        }
    }
}
