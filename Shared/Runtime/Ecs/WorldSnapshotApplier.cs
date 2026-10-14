using System.Collections.Generic;
using Delta.ECS;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.IFactories;
using SkyPirates.Shared.IServices;

namespace SkyPirates.Shared.Systems.Special
{
    internal sealed class SnapshotHistorySystem
    {
        private readonly World _world;
        private readonly IWorldHistory _history;
        private readonly ComponentId _syncIdId;
        private readonly Query _syncIdQuery;
        private readonly ForEachContextEntityAction<CollectState> _collectEntitiesAction;
        private readonly IOperation<CollectState> _collectEntitiesOperation;
        private readonly HashSet<int> _snapshotSyncIds = new();
        private readonly List<Entity> _entitiesToRetire = new();
        private readonly IEntityFactory _entityFactory;
        private readonly IEntityRegistry _entityRegistry;
        private readonly IComponentDependenciesService _componentDependencies;

        public SnapshotHistorySystem(
            World world,
            IWorldHistory history,
            IEntityFactory entityFactory,
            IEntityRegistry entityRegistry,
            IComponentDependenciesService componentDependencies)
        {
            _world = world;
            _history = history;
            _syncIdId = world.Components.GetPrimary<SyncId>();
            _syncIdQuery = world.WhereAll(_syncIdId);
            _collectEntitiesAction = CollectEntity;
            var initialState = default(CollectState);
            _collectEntitiesOperation = world.ForEachEntity(in _syncIdQuery, ref initialState, _collectEntitiesAction);
            _entityFactory = entityFactory;
            _entityRegistry = entityRegistry;
            _componentDependencies = componentDependencies;
        }

        public void ApplySnapshot(WorldData snapshot)
        {
            _snapshotSyncIds.Clear();
            foreach (var syncId in snapshot.Get<SyncId>().Values)
            {
                _snapshotSyncIds.Add(syncId.Value);
            }

            _entitiesToRetire.Clear();
            var collectState = new CollectState(_world, _syncIdId, _snapshotSyncIds, _entitiesToRetire);
            _collectEntitiesOperation.Invoke(ref collectState);
            for (int i = 0; i < _entitiesToRetire.Count; i++)
            {
                Entity entity = _entitiesToRetire[i];
                _history.RetireEntityFromSnapshot(entity);
                if (_world.Has<Alive>(entity))
                {
                    _world.Remove<Alive>(entity);
                }
            }

            foreach (var syncId in snapshot.Get<SyncId>().Values)
            {
                _entityRegistry.Reserve(syncId);
            }

            foreach (var syncIdReserve in snapshot.Get<SyncIdReserve>().Values)
            {
                _entityRegistry.Reserve(syncIdReserve);
            }

            var entitiesBySyncId = new Dictionary<int, Entity>();
            foreach (var syncId in snapshot.Get<SyncId>().Values)
            {
                Entity entity = _entityFactory.Create(new()
                {
                    SyncId = syncId,
                    RandomSeed = default,
                    SyncIdReserve = default,
                });

                entitiesBySyncId.Add(syncId.Value, entity);
            }

            _history.ApplySnapshot(snapshot, entitiesBySyncId);
            foreach (Entity entity in entitiesBySyncId.Values)
            {
                _componentDependencies.Apply(entity);
            }
        }

        private static void CollectEntity(ref CollectState state, EntityRef entity)
        {
            if (state.World.TryGet<SyncId>(entity, state.SyncIdId, out SyncId syncId)
                && !state.SnapshotSyncIds.Contains(syncId.Value))
            {
                state.EntitiesToRetire.Add(entity);
            }
        }

        private struct CollectState
        {
            public readonly World World;
            public readonly ComponentId SyncIdId;
            public readonly HashSet<int> SnapshotSyncIds;
            public readonly List<Entity> EntitiesToRetire;

            public CollectState(World world, ComponentId syncIdId, HashSet<int> snapshotSyncIds, List<Entity> entitiesToRetire)
            {
                World = world;
                SyncIdId = syncIdId;
                SnapshotSyncIds = snapshotSyncIds;
                EntitiesToRetire = entitiesToRetire;
            }
        }
    }
}
