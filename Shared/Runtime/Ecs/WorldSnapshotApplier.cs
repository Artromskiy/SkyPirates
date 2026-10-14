using System.Collections.Generic;
using Delta.ECS;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.IFactories;
using SkyPirates.Shared.IServices;

namespace SkyPirates.Shared.Ecs
{
    internal static class WorldSnapshotApplier
    {
        private static readonly ForEachContextEntityAction<CollectState> CollectEntitiesAction = CollectEntity;

        public static void Apply(
            World world,
            IWorldHistory history,
            IEntityFactory entityFactory,
            IEntityRegistry entityRegistry,
            IComponentDependenciesService componentDependencies,
            WorldData snapshot)
        {
            ComponentId syncIdId = world.Components.GetPrimary<SyncId>();
            Query syncIdQuery = world.WhereAll(syncIdId);
            var snapshotSyncIds = new HashSet<int>();
            var entitiesToRetire = new List<Entity>();
            var initialState = default(CollectState);
            IOperation<CollectState> collectEntitiesOperation =
                world.ForEachEntity(in syncIdQuery, ref initialState, CollectEntitiesAction);

            foreach (var syncId in snapshot.Get<SyncId>().Values)
            {
                snapshotSyncIds.Add(syncId.Value);
            }

            var collectState = new CollectState(world, syncIdId, snapshotSyncIds, entitiesToRetire);
            collectEntitiesOperation.Invoke(ref collectState);
            for (int i = 0; i < entitiesToRetire.Count; i++)
            {
                Entity entity = entitiesToRetire[i];
                history.RetireEntityFromSnapshot(entity);
                if (world.Has<Alive>(entity))
                {
                    world.Remove<Alive>(entity);
                }
            }

            foreach (var syncId in snapshot.Get<SyncId>().Values)
            {
                entityRegistry.Reserve(syncId);
            }

            foreach (var syncIdReserve in snapshot.Get<SyncIdReserve>().Values)
            {
                entityRegistry.Reserve(syncIdReserve);
            }

            var entitiesBySyncId = new Dictionary<int, Entity>();
            foreach (var syncId in snapshot.Get<SyncId>().Values)
            {
                Entity entity = entityFactory.Create(new()
                {
                    SyncId = syncId,
                    RandomSeed = default,
                    SyncIdReserve = default,
                });

                entitiesBySyncId.Add(syncId.Value, entity);
            }

            history.ApplySnapshot(snapshot, entitiesBySyncId);
            foreach (Entity entity in entitiesBySyncId.Values)
            {
                componentDependencies.Apply(entity);
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
