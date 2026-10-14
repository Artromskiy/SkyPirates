using Delta;
using Delta.ECS;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.Components.Framed;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.IServices.TickableExecutors;

namespace SkyPirates.Shared.Systems
{
    public class SetDestinationSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, Rotation, DesiredPosition, DesiredRotation>().Alive().NotDisabled();

        private readonly World _world;

        public SetDestinationSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var desc = _desc;
            _world.ForEach<Position, Rotation, DesiredPosition, DesiredRotation>(in desc,
                static (in Position position, in Rotation rotation, ref DesiredPosition desiredPosition, ref DesiredRotation desiredRotation) =>
                {
                    desiredPosition.Value = position;
                    desiredRotation.Value = rotation;
                }).Invoke();
        }
    }
}
