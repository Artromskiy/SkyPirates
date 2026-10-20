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
    public sealed class MultiImpactSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<BehaviourState, Damage, ImpactDistance, Position, Targets>().Alive().NotDisabled();

        private readonly World _world;
        private World _worldContext;
        private readonly IOperation<World> _applyImpacts;

        public MultiImpactSystem(World world)
        {
            _world = world;
            _worldContext = world;
            var desc = _desc;
            _applyImpacts = world.ForEach<World, BehaviourState, Damage, ImpactDistance, Position, Targets>(in desc, ref _worldContext,
                static (ref World world, in BehaviourState behaviour, in Damage damage, in ImpactDistance impactDistance, in Position position, in Targets targets) =>
                {
                    if (behaviour.State != StateId.Constants.PreAttack || behaviour.Percent != 1
                        || targets.SyncIds == null || targets.SyncIds.Count == 0)
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
                            world.GetRef<RecivedDamage>(targetEntity) += (fix)damage;
                        }
                    }
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _applyImpacts.Invoke(ref _worldContext);
        }
    }

}
