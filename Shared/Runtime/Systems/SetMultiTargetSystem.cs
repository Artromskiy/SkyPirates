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
    public class SetMultiTargetSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, Targets, TargetSearchDistance, TargetSearchPosition, TeamId>().
            Alive().NotDisabled();

        private readonly World _world;
        private ITargetSearchSystem _targetSearchContext;
        private readonly IOperation<ITargetSearchSystem> _setTargets;

        public SetMultiTargetSystem(World world, ITargetSearchSystem targetSearch)
        {
            _world = world;
            _targetSearchContext = targetSearch;
            var desc = _desc;
            _setTargets = world.ForEach<ITargetSearchSystem, TargetSearchDistance, TargetSearchPosition, Targets, TeamId>(in desc, ref _targetSearchContext,
                static (ref ITargetSearchSystem targetSearch, in TargetSearchDistance searchDistance, in TargetSearchPosition searchPosition, ref Targets target, in TeamId team) =>
                {
                    target.SyncIds ??= new();
                    target.SyncIds.Clear();
                    targetSearch.FindTargets(in searchDistance, in searchPosition, in team, target.SyncIds);
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _setTargets.Invoke(ref _targetSearchContext);
        }
    }
}
