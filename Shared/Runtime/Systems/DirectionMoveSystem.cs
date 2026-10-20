using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public class DirectionMoveSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, Direction, MaxSpeed>().Alive().NotDisabled();

        private readonly World _world;
        private fix _deltaTime;
        private readonly IOperation<fix> _move;

        public DirectionMoveSystem(World world)
        {
            _world = world;
            var desc = _desc;
            _move = world.ForEach<fix, Position, Direction, MaxSpeed>(in desc, ref _deltaTime,
                static (ref fix deltaTime, ref Position position, in Direction direction, in MaxSpeed maxSpeed) =>
                {
                    var deltaMove = ((fix2)direction * maxSpeed * deltaTime).x_y;
                    position += deltaMove;
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _deltaTime = deltaTime;
            _move.Invoke(ref _deltaTime);
        }
    }
}
