using Delta.ECS;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;

namespace SkyPirates.Shared.Systems.Special
{
    internal class DisposeSystem : IDisposeSystem
    {
        private readonly ComponentQueryCache<QuerySet> _queryCache = new();
        private Query? _toDestroyCache;
        private Query _toDestroy => _toDestroyCache ??= _world.WhereAll<Temp>();
        private readonly List<Entity> _entitiesCache = new();
        private readonly World _world;
        private readonly IWorldHistory _history;
        private readonly WorldComponentIds _componentIds;
        private readonly ComponentId _tempComponentId;
        private readonly ForEachEntityAction _disposeHistoryAction;

        public DisposeSystem(World world, IWorldHistory history)
        {
            _world = world;
            _history = history;
            _componentIds = WorldComponentIds.For(world);
            _tempComponentId = world.Components.GetPrimary<Temp>();
            _disposeHistoryAction = DisposeHistory;
        }

        public void Tick(int tick)
        {
            _entitiesCache.Clear();
            _history.CollectExpiredEntities(tick, _entitiesCache);
            foreach (var entity in _entitiesCache)
            {
                _world.Add<Temp>(entity);
            }

            var disposableComponents = _componentIds.Disposable;
            for (int i = 0; i < disposableComponents.Length; i++)
            {
                DisposeComponents(disposableComponents[i]);
            }

            var toDestroy = _toDestroy;
            _world.ForEachEntity(in toDestroy, _disposeHistoryAction).Invoke();
            _world.Destroy(in toDestroy);
        }

        private void DisposeComponents(DisposableComponentIds component)
        {
            var queries = _queryCache.Get(component.Component);
            var filter = queries.Components ??= CreateComponentQuery(component.Component);
            component.Dispose(in filter);
        }

        private void DisposeHistory(EntityRef entity) => _history.DisposeEntity(entity);

        private Query CreateComponentQuery(ComponentId componentId)
        {
            Span<ComponentId> all = stackalloc ComponentId[2] { componentId, _tempComponentId };
            return _world.WhereAll(all);
        }

        internal sealed class QuerySet
        {
            public QuerySet() { }

            public Query? Components;
        }
    }
}
