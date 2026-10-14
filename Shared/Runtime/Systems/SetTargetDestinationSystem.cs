using Delta;
using Delta.ECS;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.Components.Config;
using SkyPirates.Shared.Components.Framed;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.IServices.TickableExecutors;
using SkyPirates.Shared.Tools.Extensions;

namespace SkyPirates.Shared.Systems
{
    /// <summary>
    /// If <see href="Target"/> is set, sets desired movement and rotation to approach it.
    /// </summary>
    public sealed class SetTargetDestinationSystem : IDeltaTickableExecutor
    {
        private static readonly fix _reduceImpactDistance = fix.One / 2;
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<DesiredPosition, DesiredRotation, Position, Rotation, ImpactDistance, Target>().Alive().NotDisabled();

        private readonly World _world;

        public SetTargetDestinationSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var world = _world;
            var desc = _desc;
            _world.ForEach<World, Position, Rotation, DesiredPosition, DesiredRotation, ImpactDistance, Target>(in desc, ref world,
                static (ref World world, in Position position, in Rotation rotation, ref DesiredPosition desiredPosition, ref DesiredRotation desiredRotation, in ImpactDistance impactDistance, in Target target) =>
                {
                if (!target.Entity.HasValue)
                {
                    return;
                }

                desiredPosition.Value = position;
                desiredRotation.Value = rotation;

                fix3 targetPos = world.Get<Position>(target.Entity.Value);
                var impactReduced = impactDistance - _reduceImpactDistance;
                var impactSqrDistance = impactReduced * impactReduced;

                var sqrDistance = fix3.SqrDistance(targetPos, position);

                if (sqrDistance != 0)
                {
                    var direction = targetPos - position;
                    desiredRotation.Value = Maths.Degrees(MathsExtensions.GetRotation(direction.xz));
                }

                if (sqrDistance > impactSqrDistance)
                {
                    desiredPosition.Value = fix3.MoveTowards(targetPos, position, impactReduced);
                }
                }).Invoke(ref world);
        }
    }
}
