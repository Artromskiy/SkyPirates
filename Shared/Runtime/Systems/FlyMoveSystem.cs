using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public class FlyMoveSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, FlyDestination, MaxSpeed>().Alive().NotDisabled();

        private readonly World _world;
        private fix _deltaTime;
        private readonly IOperation<fix> _move;
        private readonly EcsResultOperation<int> _removeFinished;

        public FlyMoveSystem(World world)
        {
            _world = world;
            Query query = _desc;
            _move = world.ForEach<fix, Position, FlyDestination, MaxSpeed>(in query, ref _deltaTime,
                static (ref fix deltaTime, ref Position position, in FlyDestination fly, in MaxSpeed maxSpeed) =>
                {
                    const int ArcHeight = 4;

                    var end = fly.EndPosition;
                    var start = fly.StartPosition;
                    var endXZ = end.xz;
                    var startXZ = start.xz;
                    var currentXZ = fix2.MoveTowards(((fix3)position).xz, endXZ, deltaTime * maxSpeed);
                    var totalDistXZ = fix2.Distance(startXZ, endXZ);
                    var currentDistXZ = fix2.Distance(startXZ, currentXZ);
                    var percent = totalDistXZ == 0 ? 1 : Maths.InvLerp(0, totalDistXZ, currentDistXZ);
                    var currentY = Maths.Lerp(start.y, end.y, percent);

                    var arc = 4 * percent * (1 - percent);
                    position = new fix3(currentXZ.x, currentY + arc * ArcHeight, currentXZ.y);
                });
            _removeFinished = world.Where(in query,
                static (in Position position, in FlyDestination fly) => position == fly.EndPosition)
                .Remove<FlyDestination>();
        }

        public void Tick(int tick, fix deltaTime)
        {
            _deltaTime = deltaTime;
            _move.Invoke(ref _deltaTime);
            _removeFinished.Invoke();
        }
    }
}
