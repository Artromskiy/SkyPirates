using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IServices.TickableExecutors;
using System;
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
        private readonly List<Entity> _destroy = new();
        private Entity[] _entityBatch = Array.Empty<Entity>();
        private (fix DeltaTime, List<Impact> Impacts) _simulationState;
        private readonly IOperation<(fix DeltaTime, List<Impact> Impacts)> _simulateProjectiles;

        public ProjectileSystem(World world)
        {
            _world = world;
            _simulationState = (0, _impacts);
            var desc = _desc;
            _simulateProjectiles = world.ForEachEntity<
                (fix DeltaTime, List<Impact> Impacts), Projectile, Position, DesiredPosition, Speed, Damage>(in desc,
                ref _simulationState,
                static (ref (fix DeltaTime, List<Impact> Impacts) state, EntityRef entity, in Projectile projectile,
                    ref Position position, in DesiredPosition desiredPosition, in Speed speed, in Damage damage) =>
                {
                    fix3 destination = desiredPosition.Value;
                    fix3 current = position.Value;
                    fix step = speed.Value * state.DeltaTime;
                    bool reached = fix3.SqrDistance(current, destination) <= step * step;
                    position.Value = reached ? destination : fix3.MoveTowards(current, destination, step);
                    state.Impacts.Add(new Impact((Entity)entity, projectile.Target, position, damage, reached));
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _impacts.Clear();
            _destroy.Clear();
            _simulationState.DeltaTime = deltaTime;
            _simulateProjectiles.Invoke(ref _simulationState);

            if (_impacts.Count != 0)
            {
                EntityIdMap entityIdMap = _world.GetEntityIdMap();
                for (int i = 0; i < _impacts.Count; i++)
                {
                    var impact = _impacts[i];
                    // A projectile may outlive its target. The last destination still completes its flight.
                    bool hit = entityIdMap.TryGetEntity(impact.Target, out Entity targetEntity)
                        && _world.TryGet(targetEntity, out Position targetPosition)
                        && _world.Has<RecivedDamage>(targetEntity)
                        && fix3.SqrDistance(targetPosition, impact.Position) <= HitRadius * HitRadius;
                    if (hit)
                    {
                        _world.GetRef<RecivedDamage>(targetEntity) += (fix)impact.Damage;
                    }

                    if (hit || impact.ReachedDestination)
                    {
                        _destroy.Add(impact.Projectile);
                    }
                }
            }

            if (_destroy.Count != 0)
            {
                ReadOnlySpan<Entity> batch = Batch(_destroy);
                _world.Add<PendingDestroy>(batch);
                _world.Remove<Alive>(batch);
            }
        }

        private ReadOnlySpan<Entity> Batch(List<Entity> entities)
        {
            if (_entityBatch.Length < entities.Count)
            {
                Array.Resize(ref _entityBatch, Math.Max(entities.Count, _entityBatch.Length * 2));
            }

            entities.CopyTo(_entityBatch);
            return _entityBatch.AsSpan(0, entities.Count);
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
