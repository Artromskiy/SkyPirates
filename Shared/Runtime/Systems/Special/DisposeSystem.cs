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
        private readonly ComponentQueryCache<QuerySet> _queryCache = new();
        private Query? _toDestroyCache;
        private Query _toDestroy => _toDestroyCache ??= _world.WhereAll<Temp>();
        private readonly List<Entity> _entitiesCache = new();
        private readonly World _world;
        private readonly IWorldHistory _history;
        private readonly ComponentId[] _disposableComponents;
        private readonly ComponentId _tempComponentId;
        private readonly ForEachEntityAction _disposeHistoryAction;

        public DisposeSystem(World world, IWorldHistory history)
        {
            _world = world;
            _history = history;
            var components = new List<ComponentId>();
            var visitor = new CollectDisposableComponents(components, _queryCache);
            ReadOnlySpan<ComponentId> allComponents = world.GetGroup(Group.All);
            for (int i = 0; i < allComponents.Length; i++)
            {
                world.Components.Visit(allComponents[i], visitor);
            }

            _disposableComponents = components.ToArray();
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

            for (int i = 0; i < _disposableComponents.Length; i++)
            {
                DisposeComponents(_disposableComponents[i]);
            }

            var toDestroy = _toDestroy;
            _world.ForEachEntity(in toDestroy, _disposeHistoryAction).Invoke();
            _world.Destroy(in toDestroy);
        }

        private void DisposeComponents(ComponentId component)
        {
            var queries = _queryCache.Get(component);
            var filter = queries.Components ??= CreateComponentQuery(component);
            _world.ForEachEntity(in filter, queries.Dispose!).Invoke();
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
            public ForEachEntityAction? Dispose;
        }

        private readonly struct CollectDisposableComponents : IStructVisitor<IDisposable>
        {
            private readonly List<ComponentId> _components;
            private readonly ComponentQueryCache<QuerySet> _queryCache;

            public CollectDisposableComponents(
                List<ComponentId> components,
                ComponentQueryCache<QuerySet> queryCache)
            {
                _components = components;
                _queryCache = queryCache;
            }

            public RuntimeTypeHandle ConstraintType => typeof(IDisposable).TypeHandle;

            public void Visit<T>(ComponentId componentId) where T : struct, IDisposable
            {
                _components.Add(componentId);
                _queryCache.Get(componentId).Dispose = entity => entity.GetRef<T>(componentId).Dispose();
            }
        }
    }
}
