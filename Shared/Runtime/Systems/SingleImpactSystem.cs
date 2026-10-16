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
    public sealed class SingleImpactSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<BehaviourState, Damage, ImpactDistance, Position, Target>().WhereNone<Ranged>().Alive().NotDisabled();

        private readonly World _world;

        public SingleImpactSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var world = _world;
            var desc = _desc;
            _world.ForEach<World, BehaviourState, Damage, ImpactDistance, Position, Target>(in desc, ref world,
                static (ref World world, in BehaviourState behaviour, in Damage damage, in ImpactDistance impactDistance,
                    in Position position, in Target target) =>
                {
                    if (behaviour.State != StateId.Constants.PreAttack || behaviour.Percent != 1 || !target.SyncId.HasValue)
                        return;

                    Entity targetEntity = world.GetEntity(target.SyncId.Value);
                    var targetPosition = world.Get<Position>(targetEntity);
                    fix distance = (fix)impactDistance;
                    if (fix3.SqrDistance(targetPosition, position) <= distance * distance)
                        world.GetRef<RecivedDamage>(targetEntity) += (fix)damage;
                }).Invoke(ref world);
        }
    }
}
