using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Shared.Systems
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
