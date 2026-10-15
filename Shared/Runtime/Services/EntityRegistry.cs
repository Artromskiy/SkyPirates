using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.Systems;

namespace DVG.SkyPirates.Shared.Services
{
    public sealed class EntityRegistry : IEntityRegistry
    {
        private readonly World _world;
        private readonly EntityIdMap _ids;
        private readonly EntityCacheSystem _cache;
        private int _entityIdCounter = 1;

        public EntityRegistry(World world, EntityCacheSystem cache)
        {
            _world = world;
            _ids = world.GetEntityIdMap();
            _cache = cache;
            Rebuild();
        }

        public int NextId => _entityIdCounter;

        public void Synchronize() => _cache.Synchronize();

        public void Rebuild()
        {
            _ids.Clear();
            _entityIdCounter = 1;

            var entities = _world.WhereAll<SyncId>();
            _world.ForEachEntity(in entities, (EntityRef entity, in SyncId syncId) => Register(entity, syncId)).Invoke();

            var reserves = _world.WhereAll<SyncIdReserve>();
            _world.ForEach(in reserves, (in SyncIdReserve reserve) => Reserve(reserve)).Invoke();
            _cache.Synchronize();
        }

        public void Register(Entity entity, SyncId syncId)
        {
            _entityIdCounter = Maths.Max(_entityIdCounter, syncId.Value + 1);
            _ids.Register(entity, syncId);
        }

        public void Reserve(SyncId syncId)
        {
            _entityIdCounter = Maths.Max(_entityIdCounter, syncId.Value + 1);
        }

        public void Reserve(SyncIdReserve syncIdReserve)
        {
            int end = checked(syncIdReserve.First + syncIdReserve.Count);
            _entityIdCounter = Maths.Max(_entityIdCounter, end);
        }

        public SyncId Reserve() => new() { Value = _entityIdCounter++ };

        public SyncIdReserve Reserve(int count)
        {
            int first = _entityIdCounter;
            _entityIdCounter = checked(_entityIdCounter + count);
            return new SyncIdReserve { First = first, Count = count, Current = first };
        }

        public bool TryGet(SyncId syncId, out Entity entity)
            => _ids.TryGetEntity(syncId, out entity);
    }
}
