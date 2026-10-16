using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Tools.Extensions;

namespace DVG.SkyPirates.Shared.Systems
{
    /// <summary>
    /// Moves Entity's <see href="Position"/> and <see href="Rotation"/>
    /// with speed <see href="MaxSpeed"/> towards the desired position.
    /// </summary>
    public sealed class MoveSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, Rotation, DesiredPosition, DesiredRotation, MaxSpeed, Target>().Alive().NotDisabled();

        private readonly World _world;
        private const int RotateSpeed = 720;
        public MoveSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var delta = deltaTime;
            var desc = _desc;
            _world.ForEach<fix, Position, Rotation, DesiredPosition, DesiredRotation, MaxSpeed, Target>(in desc, ref delta,
                static (ref fix deltaTime, ref Position position, ref Rotation rotation, in DesiredPosition desiredPosition, in DesiredRotation desiredRotation, in MaxSpeed moveSpeed, in Target target) =>
                {
                    var currentPosition = (fix3)position;
                    var nextPositionXZ = fix2.MoveTowards(currentPosition.xz, desiredPosition.Value.xz, moveSpeed * deltaTime);
                    position = new fix3(nextPositionXZ.x, currentPosition.y, nextPositionXZ.y);

                    var dir = desiredPosition.Value.xz - ((fix3)position).xz;
                    var rotateTo = target.SyncId.HasValue
                        ? desiredRotation.Value
                        : fix2.SqrLength(dir) != 0
                        ? Maths.Degrees(MathsExtensions.GetRotation(dir))
                        : desiredRotation.Value;

                    rotation = Maths.RotateTowards(rotation, rotateTo, RotateSpeed * deltaTime);
                }).Invoke(ref delta);
        }
    }
}
