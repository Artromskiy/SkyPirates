using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    /// <summary>
    /// Switches <see cref="BehaviourState"/> to PreAttack if State is None and Target is in ImpactDistance
    /// </summary>
    public sealed class SinglePreAttackSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<BehaviourState, ImpactDistance, Position, Target>().Alive().NotDisabled();

        private readonly World _world;
        private World _worldContext;
        private readonly IOperation<World> _checkTargets;

        public SinglePreAttackSystem(World world)
        {
            _world = world;
            _worldContext = world;
            var desc = _desc;
            _checkTargets = world.ForEach<World, BehaviourState, ImpactDistance, Position, Target>(in desc, ref _worldContext,
                static (ref World world, ref BehaviourState behaviour, in ImpactDistance impactDistance, in Position position, in Target target) =>
                {
                    if (behaviour.State != StateId.None || !target.SyncId.HasValue)
                    {
                        return;
                    }

                    Entity targetEntity = world.GetEntity(target.SyncId.Value);
                    var targetPos = world.Get<Position>(targetEntity);
                    var sqrDistance = fix3.SqrDistance(targetPos, position);
                    var impactSqrDistance = (fix)impactDistance * impactDistance;
                    if (sqrDistance <= impactSqrDistance)
                    {
                        behaviour.ForceState = StateId.Constants.PreAttack;
                    }
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _checkTargets.Invoke(ref _worldContext);
        }
    }
}
