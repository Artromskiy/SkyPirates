using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;

namespace SlyPirates.Shared.Systems
{
    [Obsolete]
    public sealed class MarkDeadSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.WhereAll<Health>().
            Alive().NotDisabled();

        private List<Entity> _dead = new();
        private Entity[] _entityBatch = Array.Empty<Entity>();

        private readonly World _world;
        private readonly IOperation<List<Entity>> _collectDead;

        public MarkDeadSystem(World world)
        {
            _world = world;
            var desc = _desc;
            _collectDead = world.ForEachEntity<List<Entity>, Health>(in desc, ref _dead,
                static (ref List<Entity> entities, EntityRef entity, in Health health) =>
                {
                    if (health <= fix.Zero)
                    {
                        entities.Add(entity);
                    }
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _dead.Clear();
            _collectDead.Invoke(ref _dead);
            if (_dead.Count == 0)
            {
                return;
            }

            if (_entityBatch.Length < _dead.Count)
            {
                Array.Resize(ref _entityBatch, Math.Max(_dead.Count, _entityBatch.Length * 2));
            }

            _dead.CopyTo(_entityBatch);
            ReadOnlySpan<Entity> batch = _entityBatch.AsSpan(0, _dead.Count);
            _world.Add<PendingDestroy>(batch);
            _world.Remove<Alive>(batch);
        }
    }
}
