using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public sealed class CachePositionSystem : IDeltaTickableExecutor
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
