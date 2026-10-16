using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.Systems;

namespace SlyPirates.Shared.Services
{
    public sealed class EntityRegistry : IEntityRegistry
    {
        private readonly EntityIdMap _ids;
        private readonly EntityCacheSystem _cache;
        private readonly IOperation _scanIds;
        private readonly IOperation _scanReserves;
        private int _entityIdCounter = 1;
        private bool _counterNeedsRebuild;

        public EntityRegistry(World world, EntityCacheSystem cache)
        {
            _ids = world.GetEntityIdMap();
            _cache = cache;
            Query ids = world.WhereAll<SyncId>().WhereNone<PendingDestroy>();
            Query reserves = world.WhereAll<SyncIdReserve>().WhereNone<PendingDestroy>();
            _scanIds = world.ForEach(in ids, (in SyncId id) => Reserve(id));
            _scanReserves = world.ForEach(in reserves, (in SyncIdReserve reserve) => Reserve(reserve));
            Rebuild();
        }

        public int NextId
        {
            get
            {
                if (_counterNeedsRebuild)
                {
                    _scanIds.Invoke();
                    _scanReserves.Invoke();
                    _counterNeedsRebuild = false;
                }
                return _entityIdCounter;
            }
        }

        public void Synchronize() => _cache.Synchronize();

        public void Rebuild()
        {
            _cache.Synchronize();
            _entityIdCounter = 1;
            _counterNeedsRebuild = true;
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
