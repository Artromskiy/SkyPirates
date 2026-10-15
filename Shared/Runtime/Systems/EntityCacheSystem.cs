using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Shared.Systems
{
    public sealed class EntityCacheSystem : IDeltaTickableExecutor
    {
        private readonly World _world;
        private readonly EntityIdMap _ids;
        private readonly ComponentChangeOperation<SyncId> _changes;

        public EntityCacheSystem(World world)
        {
            _world = world;
            _ids = world.GetEntityIdMap();
            _changes = world.ForEachChange<SyncId>(world.Components.GetPrimary<SyncId>(), OnChanged);
        }

        public void Tick(int tick, fix deltaTime) => Synchronize();

        public void Synchronize() => _changes.Invoke();

        private void OnChanged(Entity entity, ChangeKind change, ref SyncId id)
        {
            if (change == ChangeKind.Removed)
            {
                if (_world.IsAlive(entity))
                    _ids.Deactivate(entity);
                else
                    _ids.Remove(entity);
            }
            else
            {
                _ids.Register(entity, id);
            }
        }
    }
}
