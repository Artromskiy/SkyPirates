using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Systems.Special
{
    internal class DisposeSystem : IDisposeSystem
    {
        private readonly List<Entity> _entitiesCache = new();
        private readonly World _world;
        private readonly IWorldHistory _history;
        private readonly IOperation[] _disposeOperations;
        private readonly Query _temporaryEntitiesQuery;
        private readonly IOperation _disposeHistoryOperation;

        public DisposeSystem(World world, IWorldHistory history)
        {
            _world = world;
            _history = history;
            ComponentId tempComponentId = world.Components.GetPrimary<Temp>();
            var disposeOperations = new List<IOperation>();
            var visitor = new CollectDisposableComponents(world, tempComponentId, disposeOperations);
            ReadOnlySpan<ComponentId> allComponents = world.GetGroup(Group.All);
            for (int i = 0; i < allComponents.Length; i++)
            {
                world.Components.Visit(allComponents[i], visitor);
            }

            _disposeOperations = disposeOperations.ToArray();
            _temporaryEntitiesQuery = world.WhereAll<Temp>();
            _disposeHistoryOperation = world.ForEachEntity(in _temporaryEntitiesQuery, DisposeHistory);
        }

        public void Tick(int tick)
        {
            _entitiesCache.Clear();
            _history.CollectExpiredEntities(tick, _entitiesCache);
            foreach (var entity in _entitiesCache)
            {
                _world.Add<Temp>(entity);
            }

            for (int i = 0; i < _disposeOperations.Length; i++)
            {
                _disposeOperations[i].Invoke();
            }

            _disposeHistoryOperation.Invoke();
            _world.Destroy(in _temporaryEntitiesQuery);
        }

        private void DisposeHistory(EntityRef entity) => _history.DisposeEntity(entity);

        private readonly struct CollectDisposableComponents : IStructComponentVisitor<IDisposable>
        {
            private readonly World _world;
            private readonly ComponentId _tempComponentId;
            private readonly List<IOperation> _disposeOperations;

            public CollectDisposableComponents(
                World world,
                ComponentId tempComponentId,
                List<IOperation> disposeOperations)
            {
                _world = world;
                _tempComponentId = tempComponentId;
                _disposeOperations = disposeOperations;
            }

            public void Visit<T>(ComponentId componentId) where T : struct, IDisposable
            {
                World world = _world;
                ComponentId tempComponentId = _tempComponentId;
                Query query = world.WhereAll(componentId, tempComponentId);
                _disposeOperations.Add(world.ForEachEntity(in query, (entity) =>
                    entity.GetRef<T>(componentId).Dispose()));
            }
        }
    }
}
