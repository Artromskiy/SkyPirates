using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Tools.Extensions;

namespace SlyPirates.Shared.Systems
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
        private World _worldContext;
        private readonly IOperation<World> _setDestinations;

        public SetTargetDestinationSystem(World world)
        {
            _world = world;
            _worldContext = world;
            var desc = _desc;
            _setDestinations = world.ForEach<World, Position, Rotation, DesiredPosition, DesiredRotation, ImpactDistance, Target>(in desc, ref _worldContext,
                static (ref World world, in Position position, in Rotation rotation, ref DesiredPosition desiredPosition, ref DesiredRotation desiredRotation, in ImpactDistance impactDistance, in Target target) =>
                {
                    if (!target.SyncId.HasValue)
                    {
                        return;
                    }

                    desiredPosition.Value = position;
                    desiredRotation.Value = rotation;

                    Entity targetEntity = world.GetEntity(target.SyncId.Value);
                    fix3 targetPos = world.Get<Position>(targetEntity);
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
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _setDestinations.Invoke(ref _worldContext);
        }
    }
}
