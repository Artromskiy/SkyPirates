using Delta;
using Delta.ECS;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Core.Collections;
using SkyPirates.Shared.IServices;

namespace SkyPirates.Shared.Services
{
    public class EntityRegistry : IEntityRegistry
    {
        private readonly Lookup<Entity> _idToEntity = new();
        private int _entityIdCounter = 1;

        public int NextId => _entityIdCounter;

        public void Register(Entity entity, SyncId syncId)
        {
            _entityIdCounter = Maths.Max(_entityIdCounter, syncId.Value + 1);
            _idToEntity[syncId.Value] = entity;
        }

        public void Reserve(SyncId syncId)
        {
            _entityIdCounter = Maths.Max(_entityIdCounter, syncId.Value + 1);
            if (!_idToEntity.TryGetValue(syncId.Value, out Entity entity) || entity == default)
                _idToEntity[syncId.Value] = default;
        }

        public void Reserve(SyncIdReserve syncIdReserve)
        {
            int end = checked(syncIdReserve.First + syncIdReserve.Count);
            // The high-water mark keeps future ids outside this range. A placeholder
            // map entry for every reserved child id is unnecessary.
            _entityIdCounter = Maths.Max(_entityIdCounter, end);
        }

        public SyncId Reserve()
        {
            _idToEntity[_entityIdCounter] = default;
            return new() { Value = _entityIdCounter++ };
        }

        public SyncIdReserve Reserve(int count)
        {
            int first = _entityIdCounter;
            _entityIdCounter += count;
            for (int i = first; i < count; i++)
            {
                _idToEntity[i] = default;
            }

            return new() { First = first, Count = count, Current = first };
        }

        public bool TryGet(SyncId syncId, out Entity entity)
        {
            return _idToEntity.TryGetValue(syncId.Value, out entity);
        }
    }
}
