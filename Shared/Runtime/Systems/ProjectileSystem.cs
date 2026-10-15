using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Components;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Systems
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

                    state.Impacts.Add(new Impact((Entity)entity, projectile.Target, position, damage, reached));
                }).Invoke(ref state);

            for (int i = 0; i < _impacts.Count; i++)
            {
                var impact = _impacts[i];
                Entity targetEntity = _world.GetEntity(impact.Target);
                Position targetPosition = _world.Get<Position>(targetEntity);
                bool hit = fix3.SqrDistance(targetPosition, impact.Position) <= HitRadius * HitRadius;
                if (hit)
                    _world.GetOrAdd<RecivedDamage>(targetEntity) += (fix)impact.Damage;

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
