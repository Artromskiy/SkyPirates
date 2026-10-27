using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;

namespace SlyPirates.Shared.Services
{
    public sealed class ComponentDependenciesService : IComponentDependenciesService, IDeltaTickableExecutor
    {
        private readonly World _world;
        private readonly Dependency[] _dependencies;
        private readonly HashSet<ComponentId> _expanded = new();
        private readonly HashSet<Entity> _candidates = new();
        private ComponentId[] _components = Array.Empty<ComponentId>();

        public ComponentDependenciesService(World world, ComponentDependenciesConfig dependencies)
        {
            _world = world;
            _dependencies = new Dependency[dependencies.Count];
            ComponentId alive = world.Components.GetPrimary<Alive>();

            for (int i = 0; i < _dependencies.Length; i++)
            {
                var config = dependencies[i];
                ComponentId[] has = config.Has.GetIds(world);
                ComponentId[] add = config.Add.GetIds(world);
                var queryComponents = new ComponentId[has.Length + 1];
                has.CopyTo(queryComponents, 0);
                queryComponents[has.Length] = alive;

                var dependency = new Dependency(world, has, add, world.WhereAll(queryComponents), _candidates);
                dependency.CollectCandidates = world.ForEachEntity<TickState>(in dependency.Query,
                    ref dependency.State,
                    static (ref TickState state, EntityRef entity) =>
                    {
                        for (int i = 0; i < state.Add.Length; i++)
                        {
                            if (!state.World.Has(entity, state.Add[i]))
                            {
                                state.Candidates.Add(entity);
                                return;
                            }
                        }
                    });
                _dependencies[i] = dependency;
            }
        }

        public void Tick(int tick, fix deltaTime)
        {
            _candidates.Clear();
            for (int i = 0; i < _dependencies.Length; i++)
            {
                ref Dependency dependency = ref _dependencies[i];
                dependency.CollectCandidates.Invoke(ref dependency.State);
            }

            foreach (Entity entity in _candidates)
            {
                Apply(entity);
            }
        }

        public void Apply(Entity entity)
        {
            if (_dependencies.Length == 0 || !_world.IsAlive(entity))
            {
                return;
            }

            ReadOnlySpan<ComponentId> components = Expand(entity);
            if (!components.IsEmpty)
            {
                _world.Add(entity, components);
            }
        }

        private ReadOnlySpan<ComponentId> Expand(Entity entity)
        {
            _expanded.Clear();
            bool addedComponent;
            do
            {
                addedComponent = false;
                for (int i = 0; i < _dependencies.Length; i++)
                {
                    var dependency = _dependencies[i];
                    bool hasAll = true;
                    for (int j = 0; j < dependency.Has.Length; j++)
                    {
                        ComponentId componentId = dependency.Has[j];
                        if (!_expanded.Contains(componentId) && !_world.Has(entity, componentId))
                        {
                            hasAll = false;
                            break;
                        }
                    }

                    if (!hasAll)
                    {
                        continue;
                    }

                    for (int j = 0; j < dependency.Add.Length; j++)
                    {
                        ComponentId componentId = dependency.Add[j];
                        if (!_world.Has(entity, componentId))
                        {
                            addedComponent |= _expanded.Add(componentId);
                        }
                    }
                }
            }
            while (addedComponent);

            if (_components.Length < _expanded.Count)
            {
                Array.Resize(ref _components, Math.Max(_expanded.Count, _components.Length * 2));
            }

            _expanded.CopyTo(_components);
            return _components.AsSpan(0, _expanded.Count);
        }

        private struct Dependency
        {
            public readonly ComponentId[] Has;
            public readonly ComponentId[] Add;
            public readonly Query Query;
            public TickState State;
            public IOperation<TickState> CollectCandidates;

            public Dependency(World world, ComponentId[] has, ComponentId[] add, Query query,
                HashSet<Entity> candidates)
            {
                Has = has;
                Add = add;
                Query = query;
                State = new TickState(world, add, candidates);
                CollectCandidates = default!;
            }
        }

        private struct TickState
        {
            public readonly World World;
            public readonly ComponentId[] Add;
            public readonly HashSet<Entity> Candidates;

            public TickState(World world, ComponentId[] add, HashSet<Entity> candidates)
            {
                World = world;
                Add = add;
                Candidates = candidates;
            }
        }
    }
}
