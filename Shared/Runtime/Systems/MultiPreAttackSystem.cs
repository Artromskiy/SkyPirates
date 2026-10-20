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
    public class MultiPreAttackSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<BehaviourState, ImpactDistance, Position, Targets>().Alive().NotDisabled();

        private readonly World _world;
        private World _worldContext;
        private readonly IOperation<World> _checkTargets;

        public MultiPreAttackSystem(World world)
        {
            _world = world;
            _worldContext = world;
            var desc = _desc;
            _checkTargets = world.ForEach<World, BehaviourState, ImpactDistance, Position, Targets>(in desc, ref _worldContext,
                static (ref World world, ref BehaviourState behaviour, in ImpactDistance impactDistance, in Position position, in Targets targets) =>
                {
                    if (behaviour.State != StateId.None || targets.SyncIds == null || targets.SyncIds.Count == 0)
                    {
                        return;
                    }

                    var impactSqrDistance = (fix)impactDistance * impactDistance;
                    for (int i = 0; i < targets.SyncIds.Count; i++)
                    {
                        Entity targetEntity = world.GetEntity(targets.SyncIds[i]);
                        var targetPos = world.Get<Position>(targetEntity);
                        if (fix3.SqrDistance(targetPos, position) <= impactSqrDistance)
                        {
                            behaviour.ForceState = StateId.Constants.PreAttack;
                            return;
                        }
                    }
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _checkTargets.Invoke(ref _worldContext);
        }
    }
}
