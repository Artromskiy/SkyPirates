using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Shared.Systems
{
    public sealed class MultiImpactSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<BehaviourState, Damage, ImpactDistance, Position, Targets>().Alive().NotDisabled();

        private readonly World _world;

        public MultiImpactSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var world = _world;
            var desc = _desc;
            _world.ForEach(in desc, ref world,
                static (ref World world, in BehaviourState behaviour, in Damage damage, in ImpactDistance impactDistance, in Position position, in Targets targets) =>
                {
                    if (behaviour.State != StateId.Constants.PreAttack || behaviour.Percent != 1)
                    {
                        return;
                    }

                    if (targets.Entities == null || targets.Entities.Count == 0)
                    {
                        return;
                    }

                    var impactSqrDistance = (fix)impactDistance * impactDistance;
                    for (int i = 0; i < targets.Entities.Count; i++)
                    {
                        var targetPos = world.Get<Position>(targets.Entities[i]);
                        var sqrDistance = fix3.SqrDistance(targetPos, position);

                        if (sqrDistance > impactSqrDistance)
                        {
                            continue;
                        }

                        world.GetRef<RecivedDamage>(targets.Entities[i]) += (fix)damage;
                    }
                }).Invoke(ref world);
        }
    }

}
