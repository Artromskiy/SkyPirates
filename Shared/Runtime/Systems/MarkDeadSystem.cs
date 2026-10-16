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

        private readonly List<Entity> _dead = new();

        private readonly World _world;

        public MarkDeadSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            _dead.Clear();
            var dead = _dead;
            var desc = _desc;
            _world.ForEachEntity<List<Entity>, Health>(in desc, ref dead,
                static (ref List<Entity> entities, EntityRef entity, in Health health) =>
                {
                    if (health <= fix.Zero)
                    {
                        entities.Add(entity);
                    }
                }).Invoke(ref dead);
            foreach (var item in _dead)
            {
                _world.Add<PendingDestroy>(item);
                _world.Remove<Alive>(item);
            }
        }
    }
}
