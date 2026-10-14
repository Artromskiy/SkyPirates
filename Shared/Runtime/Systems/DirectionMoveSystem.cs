using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Shared.Systems
{
    public class DirectionMoveSystem : ITransientDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, Direction, MaxSpeed>().Alive().NotDisabled();

        private readonly World _world;
        public DirectionMoveSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var delta = deltaTime;
            var desc = _desc;
            _world.ForEach<fix, Position, Direction, MaxSpeed>(in desc, ref delta,
                static (ref fix deltaTime, ref Position position, in Direction direction, in MaxSpeed maxSpeed) =>
                {
                    var deltaMove = ((fix2)direction * maxSpeed * deltaTime).x_y;
                    position += deltaMove;
                }).Invoke(ref delta);
        }
    }
}
