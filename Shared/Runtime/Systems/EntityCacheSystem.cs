using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public sealed class EntityCacheSystem : IDeltaTickableExecutor
    {
        private readonly World _world;
        private readonly EntityIdMap _ids;
        private readonly ComponentChangeOperation<SyncId> _changes;
        private readonly ComponentChangeOperation<PendingDestroy> _lifetime;

        public EntityCacheSystem(World world)
        {
            _world = world;
            _ids = world.GetEntityIdMap();
            _changes = world.ForEachChange<SyncId>(world.Components.GetPrimary<SyncId>(), OnChanged);
            _lifetime = world.ForEachChange<PendingDestroy>(world.Components.GetPrimary<PendingDestroy>(), OnLifetimeChanged);
        }

        public void Tick(int tick, fix deltaTime) => Synchronize();

        public void Synchronize()
        {
            _changes.Invoke();
            _lifetime.Invoke();
        }

        private void OnLifetimeChanged(Entity entity, ChangeKind change, ref PendingDestroy pending)
        {
            if (!_world.IsAlive(entity))
            {
                _ids.Remove(entity);
            }
            else if (_world.Has<PendingDestroy>(entity))
            {
                _ids.Deactivate(entity);
            }
            else if (_world.TryGet(entity, out SyncId id))
            {
                _ids.Register(entity, id);
            }
        }

        private void OnChanged(Entity entity, ChangeKind change, ref SyncId id)
        {
            if (change == ChangeKind.Removed)
            {
                if (_world.IsAlive(entity))
                {
                    _ids.Deactivate(entity);
                }
                else
                {
                    _ids.Remove(entity);
                }
            }
            else
            {
                _ids.Register(entity, id);
                if (_world.Has<PendingDestroy>(entity))
                {
                    _ids.Deactivate(entity);
                }
            }
        }
    }
}
