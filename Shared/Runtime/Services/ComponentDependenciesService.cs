using Delta.ECS;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.IServices;
using System.Collections.Generic;

namespace SkyPirates.Shared.Services
{
    public class ComponentDependenciesService : IComponentDependenciesService
    {
        private readonly World _world;
        private readonly RuntimeDependency[] _dependencies;

        public ComponentDependenciesService(World world, ComponentDependenciesConfig dependencies)
        {
            _world = world;
            _dependencies = new RuntimeDependency[dependencies.Count];
            for (int i = 0; i < _dependencies.Length; i++)
            {
                var dependency = dependencies[i];
                ComponentId[] has = dependency.Has.GetIds(world);
                ComponentId[] add = dependency.Add.GetIds(world);
                _dependencies[i] = new RuntimeDependency(has, add);
            }
        }

        public void Apply(Entity entity) => ApplyDependencies(entity);

        private void ApplyDependencies(Entity entity)
        {
            bool addedComponent;
            do
            {
                addedComponent = false;
                for (int i = 0; i < _dependencies.Length; i++)
                {
                    RuntimeDependency dependency = _dependencies[i];
                    bool hasAll = true;
                    for (int j = 0; j < dependency.Has.Length; j++)
                    {
                        if (!_world.Has(entity, dependency.Has[j]))
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
                            _world.Add(entity, componentId);
                            addedComponent = true;
                        }
                    }
                }
            }
            while (addedComponent);
        }

        private readonly struct RuntimeDependency
        {
            public readonly ComponentId[] Has;
            public readonly ComponentId[] Add;

            public RuntimeDependency(ComponentId[] has, ComponentId[] add)
            {
                Has = has;
                Add = add;
            }
        }
    }
}
