using System.Collections.Generic;
using Delta.ECS;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0x2ECC2E2A3A426A48UL)]
    [ComponentAlias("EntityIdMap")]
    public readonly struct EntityIdMap
    {
        private readonly Dictionary<int, CachedEntity> _entities;
        private readonly Dictionary<int, Entry> _ids;

        public EntityIdMap(int initialCapacity)
        {
            _entities = new Dictionary<int, CachedEntity>(initialCapacity);
            _ids = new Dictionary<int, Entry>(initialCapacity);
        }

        public Entity GetEntity(SyncId id)
        {
            CachedEntity cached = _entities[id.Value];
            if (!cached.Present)
                throw new KeyNotFoundException($"SyncId {id.Value} has no active cached entity.");
            return cached.Entity;
        }

        internal bool TryGetEntity(SyncId id, out Entity entity)
        {
            if (_entities.TryGetValue(id.Value, out CachedEntity cached) && cached.Present)
            {
                entity = cached.Entity;
                return true;
            }

            entity = default;
            return false;
        }

        internal bool TryGetRetainedEntity(SyncId id, out Entity entity)
        {
            if (_entities.TryGetValue(id.Value, out CachedEntity cached))
            {
                entity = cached.Entity;
                return true;
            }

            entity = default;
            return false;
        }

        internal void Register(Entity entity, SyncId id)
        {
            if (_ids.TryGetValue(entity.Index, out Entry previousEntry))
            {
                if (previousEntry.Present && previousEntry.Id.Value == id.Value && previousEntry.Entity == entity)
                    return;

                Remove(previousEntry.Entity);
            }

            if (_entities.TryGetValue(id.Value, out CachedEntity previous))
                Remove(previous.Entity);

            _entities[id.Value] = new CachedEntity(entity, true);
            _ids[entity.Index] = new Entry(entity, id, true);
        }

        internal void Deactivate(Entity entity)
        {
            if (!_ids.TryGetValue(entity.Index, out Entry entry) || !entry.Present || entry.Entity != entity)
                return;

            _entities[entry.Id.Value] = new CachedEntity(entity, false);
            _ids[entity.Index] = new Entry(entity, entry.Id, false);
        }

        internal void Remove(Entity entity)
        {
            if (!_ids.TryGetValue(entity.Index, out Entry entry) || entry.Entity != entity)
                return;

            _entities.Remove(entry.Id.Value);
            _ids.Remove(entity.Index);
        }

        internal void Clear()
        {
            _entities.Clear();
            _ids.Clear();
        }


        private readonly struct CachedEntity
        {
            public readonly Entity Entity;
            public readonly bool Present;

            public CachedEntity(Entity entity, bool present)
            {
                Entity = entity;
                Present = present;
            }
        }

        private readonly struct Entry
        {
            public readonly Entity Entity;
            public readonly SyncId Id;
            public readonly bool Present;

            public Entry(Entity entity, SyncId id, bool present)
            {
                Entity = entity;
                Id = id;
                Present = present;
            }
        }
    }
}
