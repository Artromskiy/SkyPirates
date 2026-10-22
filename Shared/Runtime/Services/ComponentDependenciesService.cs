using Delta.ECS;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IServices;
using System.Collections.Generic;

namespace SlyPirates.Shared.Services
{
    public class ComponentDependenciesService : IComponentDependenciesService
    {
        private readonly World _world;
        private readonly (ComponentId[] Has, ComponentId[] Add)[] _dependencies;
        private readonly HashSet<ComponentId> _expanded = new();

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

            _expanded.Clear();
            foreach (ComponentId componentId in _world.Components.GetComponentIds())
            {
                if (_world.Has(entity, componentId))
                {
                    _expanded.Add(componentId);
                }
            }

            ComponentId[] componentIds = Expand();
            _world.Add(entity, componentIds);
        }

        private ComponentId[] Expand()
        {
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
                        if (!_expanded.Contains(dependency.Has[j]))
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
                        addedComponent |= _expanded.Add(dependency.Add[j]);
                    }
                }
            }
            while (addedComponent);

            var result = new ComponentId[_expanded.Count];
            _expanded.CopyTo(result);
            return result;
        }
    }
}
