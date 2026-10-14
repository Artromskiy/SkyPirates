using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Systems
{
    [Obsolete]
    public sealed class MarkDeadSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.WhereAll<Health>().
            Alive().NotDisabled();

        private readonly List<Entity> _dead = new();

        private readonly World _world;
        private readonly IWorldHistory _history;

        public MarkDeadSystem(World world, IWorldHistory history)
        {
            _world = world;
            _history = history;
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
                _history.MarkForDisposal(item, tick);
                _world.Remove<Alive>(item);
            }
        }
    }
}
