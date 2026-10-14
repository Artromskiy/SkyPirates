using Delta;
using Delta.ECS;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.Components.Config;
using SkyPirates.Shared.Components.Framed;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Ids;
using SkyPirates.Shared.IServices.TickableExecutors;

namespace SkyPirates.Shared.Systems
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
                    if (behaviour.State != StateId.Constants.PreAttack || behaviour.Percent != 1 || !target.Entity.HasValue)
                        return;

                    var targetPosition = world.Get<Position>(target.Entity.Value);
                    fix distance = (fix)impactDistance;
                    if (fix3.SqrDistance(targetPosition, position) <= distance * distance)
                        world.GetRef<RecivedDamage>(target.Entity.Value) += (fix)damage;
                }).Invoke(ref world);
        }
    }
}
