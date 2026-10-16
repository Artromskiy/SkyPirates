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

        public MultiPreAttackSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var world = _world;
            var desc = _desc;
            _world.ForEach<World, BehaviourState, ImpactDistance, Position, Targets>(in desc, ref world,
                static (ref World world, ref BehaviourState behaviour, in ImpactDistance impactDistance, in Position position, in Targets targets) =>
                {
                if (behaviour.State != StateId.None)
                {
                    return;
                }

                if (targets.SyncIds == null || targets.SyncIds.Count == 0)
                {
                    return;
                }

                var impactSqrDistance = (fix)impactDistance * impactDistance;

                for (int i = 0; i < targets.SyncIds.Count; i++)
                {
                    Entity targetEntity = world.GetEntity(targets.SyncIds[i]);
                    var targetPos = world.Get<Position>(targetEntity);
                    var sqrDistance = fix3.SqrDistance(targetPos, position);
                    if (sqrDistance <= impactSqrDistance)
                    {
                        behaviour.ForceState = StateId.Constants.PreAttack;
                        return;
                    }
                }
                }).Invoke(ref world);
        }
    }
}
