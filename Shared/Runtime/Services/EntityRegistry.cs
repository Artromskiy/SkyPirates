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
        private readonly EntityIdMap _ids;
        private readonly EntityCacheSystem _cache;
        private readonly IOperation _scanIds;
        private readonly IOperation _scanReserves;
        private int _entityIdCounter = 1;

        public EntityRegistry(World world, EntityCacheSystem cache)
        {
            _ids = world.GetEntityIdMap();
            _cache = cache;
            Query ids = world.WhereAll<SyncId>().WhereNone<PendingDestroy>();
            Query reserves = world.WhereAll<SyncIdReserve>().WhereNone<PendingDestroy>();
            _scanIds = world.ForEachEntity(in ids, (EntityRef entity, in SyncId id) => Register(entity, id));
            _scanReserves = world.ForEach(in reserves, (in SyncIdReserve reserve) => Reserve(reserve));
            Rebuild();
        }

        public int NextId => _entityIdCounter;

        public void Synchronize() => _cache.Synchronize();

        public void Rebuild()
        {
            _cache.Synchronize();
            _entityIdCounter = 1;

            _scanIds.Invoke();
            _scanReserves.Invoke();
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

        public bool TryGet(SyncId syncId, out Entity entity)
            => _ids.TryGetEntity(syncId, out entity);
    }
}
