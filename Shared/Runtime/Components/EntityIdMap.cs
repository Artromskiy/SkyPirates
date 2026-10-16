using System.Collections.Generic;
using Delta.ECS;

namespace SlyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x2ECC2E2A3A426A48UL)]
    [ComponentAlias("EntityIdMap")]
    public readonly struct EntityIdMap
    {
        private readonly Dictionary<int, (Entity Entity, bool Present)> _entities;
        private readonly Dictionary<int, (Entity Entity, SyncId Id, bool Present)> _ids;

        public EntityIdMap(int initialCapacity)
        {
            _entities = new Dictionary<int, (Entity, bool)>(initialCapacity);
            _ids = new Dictionary<int, (Entity, SyncId, bool)>(initialCapacity);
        }

        public Entity GetEntity(SyncId id)
        {
            var cached = _entities[id.Value];
            if (!cached.Present)
                throw new KeyNotFoundException($"SyncId {id.Value} has no active cached entity.");
            return cached.Entity;
        }

        public bool TryGetEntity(SyncId id, out Entity entity)
        {
            if (_entities.TryGetValue(id.Value, out var cached) && cached.Present)
            {
                entity = cached.Entity;
                return true;
            }

            entity = default;
            return false;
        }

        public bool TryGetRetainedEntity(SyncId id, out Entity entity)
        {
            if (_entities.TryGetValue(id.Value, out var cached))
            {
                entity = cached.Entity;
                return true;
            }

            entity = default;
            return false;
        }

        internal void Register(Entity entity, SyncId id)
        {
            if (_ids.TryGetValue(entity.Index, out var previousEntry))
            {
                if (previousEntry.Present && previousEntry.Id.Value == id.Value && previousEntry.Entity == entity)
                    return;

                Remove(previousEntry.Entity);
            }

            if (_entities.TryGetValue(id.Value, out var previous))
                Remove(previous.Entity);

            _entities[id.Value] = (entity, true);
            _ids[entity.Index] = (entity, id, true);
        }

        internal void Deactivate(Entity entity)
        {
            if (!_ids.TryGetValue(entity.Index, out var entry) || !entry.Present || entry.Entity != entity)
                return;

            _entities[entry.Id.Value] = (entity, false);
            _ids[entity.Index] = (entity, entry.Id, false);
        }

        internal void Remove(Entity entity)
        {
            if (!_ids.TryGetValue(entity.Index, out var entry) || entry.Entity != entity)
                return;

            _entities.Remove(entry.Id.Value);
            _ids.Remove(entity.Index);
        }
    }
}
