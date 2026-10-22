using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public class FramedComponentsSystem : IDeltaTickableExecutor
    {
        private readonly World _world;
        private readonly DependencyData[] _dependencies;
        private readonly ComponentId[] _framedComponentIds;
        private readonly IOperation[] _clearOperations;

        public FramedComponentsSystem(FramedComponentDependenciesConfig componentDependencies, World world)
        {
            _world = world;
            _framedComponentIds = world.GetGroup(Group.Temporary).ToArray();
            _clearOperations = new IOperation[_framedComponentIds.Length];
            for (int i = 0; i < _framedComponentIds.Length; i++)
            {
                var query = world.WhereAll(_framedComponentIds[i]);
                _clearOperations[i] = world.ForEach(in query, _framedComponentIds[i], typeof(ClearFramedComponent<>));
            }
            _dependencies = componentDependencies.ConvertAll(dependency => CreateDependency(world, dependency)).ToArray();
        }

        public void Tick(int tick, fix deltaTime)
        {
            for (int i = 0; i < _framedComponentIds.Length; i++)
            {
                _clearOperations[i].Invoke();
            }

            foreach (var data in _dependencies)
            {
                foreach (var query in data.AddQueries)
                {
                    _world.Add(in query, data.AddComponentIds);
                }
            }
        }

        internal struct ClearFramedComponent<T> : IForEach where T : struct
        {
            public void Invoke(ref T component) => component = default;
        }

        private static DependencyData CreateDependency(World world, ComponentDependenciesData dependency)
        {
            var configuredHas = dependency.Has.GetIds(world);
            var has = new ComponentId[configuredHas.Length + 1];
            configuredHas.CopyTo(has, 0);
            has[configuredHas.Length] = world.Components.GetPrimary<Alive>();

            var addIds = dependency.Add.GetIds(world);
            var queries = new Query[addIds.Length];
            var filter = world.WhereAll(has);
            for (int i = 0; i < addIds.Length; i++)
            {
                queries[i] = filter.WhereNone(addIds[i]);
            }

            return new DependencyData(addIds, queries);
        }

        internal readonly struct DependencyData
        {
            public readonly ComponentId[] AddComponentIds;
            public readonly Query[] AddQueries;

            public DependencyData(ComponentId[] addComponentIds, Query[] addQueries)
            {
                AddComponentIds = addComponentIds;
                AddQueries = addQueries;
            }
        }
    }
}
