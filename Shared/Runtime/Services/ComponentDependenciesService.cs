using Delta.ECS;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IServices;
using System;
using System.Collections.Generic;

namespace SlyPirates.Shared.Services
{
    public class ComponentDependenciesService : IComponentDependenciesService
    {
        private readonly World _world;
        private readonly (ComponentId[] Has, ComponentId[] Add)[] _dependencies;
        private readonly HashSet<ComponentId> _expanded = new();
        private ComponentId[] _components = Array.Empty<ComponentId>();

        public ComponentDependenciesService(World world, ComponentDependenciesConfig dependencies)
        {
            _world = world;
            _dependencies = new (ComponentId[], ComponentId[])[dependencies.Count];

            for (int i = 0; i < _dependencies.Length; i++)
            {
                var dependency = dependencies[i];
                _dependencies[i] = (dependency.Has.GetIds(world), dependency.Add.GetIds(world));
            }
        }

        public void Apply(Entity entity)
        {
            if (_dependencies.Length == 0)
            {
                return;
            }

            if (!_world.IsAlive(entity))
            {
                return;
            }

            _world.Add(entity, Expand(entity));
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
                        if (!_expanded.Contains(dependency.Has[j]) && !_world.Has(entity, dependency.Has[j]))
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
                        if (!_world.Has(entity, dependency.Add[j]))
                        {
                            addedComponent |= _expanded.Add(dependency.Add[j]);
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
    }
}
