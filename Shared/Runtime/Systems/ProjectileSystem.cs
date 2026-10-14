using Delta;
using Delta.ECS;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Components.Config;
using SkyPirates.Shared.Components.Framed;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace SkyPirates.Shared.Systems
{
    public sealed class ProjectileSystem : IDeltaTickableExecutor
    {
        private static readonly fix HitRadius = fix.One / 2;

        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Projectile, Position, DesiredPosition, Speed, Damage>().Alive().NotDisabled();

        private readonly World _world;
        private readonly IWorldHistory _history;
        private readonly List<Impact> _impacts = new();

        public ProjectileSystem(World world, IWorldHistory history)
        {
            _world = world;
            _history = history;
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

                    Entity targetEntity = projectile.Target;
                    state.Impacts.Add(new Impact((Entity)entity, targetEntity, position, damage,
                        targetEntity.IsValid, reached));
                }).Invoke(ref state);

            for (int i = 0; i < _impacts.Count; i++)
            {
                var impact = _impacts[i];
                bool hit = impact.HasTarget
                    && _world.IsAlive(impact.Target)
                    && _world.TryGet(impact.Target, out Position targetPosition)
                    && fix3.SqrDistance(targetPosition, impact.Position) <= HitRadius * HitRadius;
                if (hit)
                    _world.GetOrAdd<RecivedDamage>(impact.Target) += (fix)impact.Damage;

                if (hit || impact.ReachedDestination)
                {
                    _history.MarkForDisposal(impact.Projectile, tick);
                    _world.Remove<Alive>(impact.Projectile);
                }
            }
        }

        private readonly struct Impact
        {
            public readonly Entity Projectile;
            public readonly Entity Target;
            public readonly fix3 Position;
            public readonly Damage Damage;
            public readonly bool HasTarget;
            public readonly bool ReachedDestination;

            public Impact(Entity projectile, Entity target, fix3 position, Damage damage, bool hasTarget,
                bool reachedDestination)
            {
                Projectile = projectile;
                Target = target;
                Position = position;
                Damage = damage;
                HasTarget = hasTarget;
                ReachedDestination = reachedDestination;
            }
        }
    }
}
