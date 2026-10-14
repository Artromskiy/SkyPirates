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
    /// <summary>
    /// Switches <see cref="BehaviourState"/> to PreAttack if State is None and Target is in ImpactDistance
    /// </summary>
    public sealed class SinglePreAttackSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<BehaviourState, ImpactDistance, Position, Target>().Alive().NotDisabled();

        private readonly World _world;

        public SinglePreAttackSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var world = _world;
            var desc = _desc;
            _world.ForEach<World, BehaviourState, ImpactDistance, Position, Target>(in desc, ref world,
                static (ref World world, ref BehaviourState behaviour, in ImpactDistance impactDistance, in Position position, in Target target) =>
                {
                if (behaviour.State != StateId.None)
                {
                    return;
                }

                if (!target.Entity.HasValue)
                {
                    return;
                }

                var targetPos = world.Get<Position>(target.Entity.Value);
                var sqrDistance = fix3.SqrDistance(targetPos, position);
                var impactSqrDistance = (fix)impactDistance * impactDistance;

                if (sqrDistance > impactSqrDistance)
                {
                    return;
                }

                behaviour.ForceState = StateId.Constants.PreAttack;
                }).Invoke(ref world);
        }
    }
}
