using DVG.SkyPirates.Shared.Core;
using Delta.ECS;
using DVG.SkyPirates.Shared.Components;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Components.Framed;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DVG.SkyPirates.Shared.Ecs
{
    internal static class WorldExtensions
    {
        public static Query Alive(this Query query) => query.WhereAll<Alive>();

        public static Query NotDisabled(this Query query) => query.WhereNone<Disabled>();

        public static ref T GetOrAdd<T>(this World world, Entity entity) where T : struct
        {
            if (!world.Has<T>(entity))
            {
                world.Add<T>(entity);
            }

            return ref world.GetRef<T>(entity);
        }

    }

    internal sealed class WorldComponentIds
    {
        private static readonly ConditionalWeakTable<World, WorldComponentIds> Cache = new();

        public readonly ComponentId[] History;
        public readonly ComponentId[] Framed;
        public readonly DisposableComponentIds[] Disposable;

        private WorldComponentIds(World world)
        {
            ReadOnlySpan<ComponentId> history = world.GetGroup(Group.History);
            History = history.ToArray();

            ReadOnlySpan<ComponentId> framed = world.GetGroup(Group.Temporary);
            Framed = framed.ToArray();

            var disposable = new List<DisposableComponentIds>();
            var disposableAction = new CollectDisposableIds(world, disposable);
            DisposableComponentsRegistry.ForEachData(ref disposableAction);
            Disposable = disposable.ToArray();
        }

        public static WorldComponentIds For(World world)
            => Cache.GetValue(world, static owner => new WorldComponentIds(owner));

        private readonly struct CollectDisposableIds : IStructGenericAction<IDisposable>
        {
            private readonly World _world;
            private readonly List<DisposableComponentIds> _entries;

            public CollectDisposableIds(World world, List<DisposableComponentIds> entries)
            {
                _world = world;
                _entries = entries;
            }

            public void Invoke<T>() where T : struct, IDisposable
                => _entries.Add(new DisposableComponentIds(_world.Components.GetPrimary<T>(), new ComponentDisposer<T>(_world)));
        }
    }

    internal readonly struct DisposableComponentIds
    {
        public readonly ComponentId Component;
        private readonly IComponentDisposer _disposer;

        public DisposableComponentIds(ComponentId component, IComponentDisposer disposer)
        {
            Component = component;
            _disposer = disposer;
        }

        public void Dispose(in Query filter) => _disposer.Dispose(in filter);
    }

    internal interface IComponentDisposer
    {
        void Dispose(in Query filter);
    }

    internal sealed class ComponentDisposer<T> : IComponentDisposer where T : struct, IDisposable
    {
        private readonly World _world;
        private readonly ForEachEntityAction _disposeEntity;

        public ComponentDisposer(World world)
        {
            _world = world;
            _disposeEntity = DisposeEntity;
        }

        public void Dispose(in Query filter) => _world.ForEachEntity(in filter, _disposeEntity).Invoke();

        private void DisposeEntity(EntityRef entity) => _world.GetRef<T>(entity).Dispose();
    }

    internal sealed class ComponentQueryCache<TQueries> where TQueries : class, new()
    {
        private readonly Dictionary<Type, TQueries> _queries = new();
        private readonly Dictionary<ComponentId, TQueries> _queriesById = new();

        public TQueries Get<T>() where T : struct
        {
            var componentType = typeof(T);
            if (!_queries.TryGetValue(componentType, out var queries))
            {
                _queries.Add(componentType, queries = new TQueries());
            }

            return queries;
        }

        public TQueries Get(ComponentId componentId)
        {
            if (!_queriesById.TryGetValue(componentId, out var queries))
            {
                _queriesById.Add(componentId, queries = new TQueries());
            }

            return queries;
        }
    }
}
