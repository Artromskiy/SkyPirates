using Delta.ECS;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IServices;

namespace SlyPirates.Shared.Services
{
    public class ComponentDependenciesService : IComponentDependenciesService
    {
        private readonly World _world;
        private readonly (ComponentId[] Has, ComponentId[] Add)[] _dependencies;

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
                        if (!_world.Has(entity, dependency.Has[j]))
                        {
                            hasAll = false;
                            break;
                        }
                    }

                    if (hasAll)
                        addedComponent |= _world.Add(entity, dependency.Add);
                }
            }
            while (addedComponent);
        }
    }
}
