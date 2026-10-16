using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace SlyPirates.Shared.Systems
{
    public sealed class ProjectileSystem : IDeltaTickableExecutor
    {
        private static readonly fix HitRadius = fix.One / 2;

        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Projectile, Position, DesiredPosition, Speed, Damage>().Alive().NotDisabled();

        private readonly World _world;
        private readonly List<Impact> _impacts = new();

        public ProjectileSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            _impacts.Clear();
            var state = (deltaTime, _impacts);
            var desc = _desc;
            _world.ForEachEntity<(fix DeltaTime, List<Impact> Impacts), Projectile, Position, DesiredPosition, Speed, Damage>(
                in desc, ref state,
                static (ref (fix DeltaTime, List<Impact> Impacts) state, EntityRef entity, in Projectile projectile,
                    ref Position position, in DesiredPosition desiredPosition, in Speed speed,
                    in Damage damage) =>
                {
                    fix3 destination = desiredPosition.Value;
                    fix3 current = position.Value;
                    fix step = speed.Value * state.DeltaTime;
                    bool reached = fix3.SqrDistance(current, destination) <= step * step;
                    position.Value = reached ? destination : fix3.MoveTowards(current, destination, step);

                    state.Impacts.Add(new Impact((Entity)entity, projectile.Target, position, damage, reached));
                }).Invoke(ref state);

            for (int i = 0; i < _impacts.Count; i++)
            {
                var impact = _impacts[i];
                // A projectile may outlive its target. The last destination still completes its flight.
                bool hit = _world.GetEntityIdMap().TryGetEntity(impact.Target, out Entity targetEntity)
                    && _world.TryGet(targetEntity, out Position targetPosition)
                    && fix3.SqrDistance(targetPosition, impact.Position) <= HitRadius * HitRadius;
                if (hit)
                    _world.GetOrAdd<RecivedDamage>(targetEntity) += (fix)impact.Damage;

                if (hit || impact.ReachedDestination)
                {
                    _world.Add<PendingDestroy>(impact.Projectile);
                    _world.Remove<Alive>(impact.Projectile);
                }
            }
        }

        private readonly struct Impact
        {
            public readonly Entity Projectile;
            public readonly SyncId Target;
            public readonly fix3 Position;
            public readonly Damage Damage;
            public readonly bool ReachedDestination;

            public Impact(Entity projectile, SyncId target, fix3 position, Damage damage, bool reachedDestination)
            {
                Projectile = projectile;
                Target = target;
                Position = position;
                Damage = damage;
                ReachedDestination = reachedDestination;
            }
        }
    }
}
