using Delta;
using Delta.ECS;
using DVG.Components;
using DVG.Core.Collections;
using DVG.SkyPirates.Shared.IServices;

namespace DVG.SkyPirates.Shared.Services
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
            _idToEntity[syncId.Value] = default;
        }

        public void Reserve(SyncIdReserve syncIdReserve)
        {
            int end = checked(syncIdReserve.First + syncIdReserve.Count);
            for (int i = syncIdReserve.First; i < end; i++)
            {
                _idToEntity[i] = default;
            }

            _entityIdCounter = Maths.Max(_entityIdCounter, syncIdReserve.First + syncIdReserve.Count);
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
