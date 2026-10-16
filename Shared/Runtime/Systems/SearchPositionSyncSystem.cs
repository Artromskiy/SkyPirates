using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public class SearchPositionSyncSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<TargetSearchPosition, Position>().Alive().NotDisabled();

        private readonly World _world;

        public SearchPositionSyncSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var desc = _desc;
            _world.ForEach<TargetSearchPosition, Position>(in desc,
                static (ref TargetSearchPosition searchPosition, in Position position) => searchPosition = (fix3)position).Invoke();
        }
    }
}
