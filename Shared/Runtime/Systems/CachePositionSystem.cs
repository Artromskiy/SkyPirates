using Delta;
using Delta.ECS;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.Components.Framed;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.IServices.TickableExecutors;

namespace SkyPirates.Shared.Systems
{
    public sealed class CachePositionSystem : ITransientDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, CachePosition>().Alive().NotDisabled();

        private readonly World _world;

        public CachePositionSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var desc = _desc;
            _world.ForEach<Position, CachePosition>(in desc,
                static (in Position position, ref CachePosition cachePosition) => cachePosition = (fix3)position).Invoke();
        }
    }
}
