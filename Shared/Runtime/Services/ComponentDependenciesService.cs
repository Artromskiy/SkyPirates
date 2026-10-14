using Delta.ECS;
using DVG.SkyPirates.Shared.Components;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Services
{
    public class ComponentDependenciesService : IComponentDependenciesService
    {
        private readonly World _world;
        private readonly RuntimeDependency[] _dependencies;
        private readonly IComponentChangeOperation[] _changeOperations;
        private readonly List<Entity> _entitiesToProcess = new();
        private readonly HashSet<Entity> _processedEntities = new();

        public ComponentDependenciesService(World world, ComponentDependenciesConfig dependencies)
        {
            _world = world;
            _dependencies = new RuntimeDependency[dependencies.Count];
            var watchedComponentIds = new HashSet<ComponentId>();
            ComponentId syncId = world.Components.GetPrimary<SyncId>();

            for (int i = 0; i < _dependencies.Length; i++)
            {
                var dependency = dependencies[i];
                ComponentId[] has = dependency.Has.GetIds(world);
                ComponentId[] add = dependency.Add.GetIds(world);
                _dependencies[i] = new RuntimeDependency(has, add);

                if (has.Length == 0)
                {
                    watchedComponentIds.Add(syncId);
                }
                else
                {
                    for (int j = 0; j < has.Length; j++)
                    {
                        watchedComponentIds.Add(has[j]);
                    }
                }
            }

            var operations = new List<IComponentChangeOperation>(watchedComponentIds.Count);
            var visitor = new CreateChangeOperationVisitor(world, operations, this);
            foreach (ComponentId componentId in watchedComponentIds)
            {
                world.Components.Visit(componentId, visitor);
            }

            _changeOperations = operations.ToArray();
        }

        public void ProcessChanges()
        {
            _entitiesToProcess.Clear();
            _processedEntities.Clear();

            for (int i = 0; i < _changeOperations.Length; i++)
            {
                _changeOperations[i].Invoke();
            }

            for (int i = 0; i < _entitiesToProcess.Count; i++)
            {
                ApplyDependencies(_entitiesToProcess[i]);
            }
        }

        private void OnComponentChanged<T>(Entity entity, ChangeKind change, ref T component) where T : struct
        {
            if (change == ChangeKind.Removed)
            {
                return;
            }

            if (_processedEntities.Add(entity))
            {
                _entitiesToProcess.Add(entity);
            }
        }

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

        private interface IComponentChangeOperation
        {
            void Invoke();
        }

        private sealed class ChangeOperation<T> : IComponentChangeOperation where T : struct
        {
            private readonly ComponentChangeOperation<T> _operation;

            public ChangeOperation(World world, ComponentId componentId, ComponentDependenciesService owner)
            {
                _operation = world.ForEachChange<T>(componentId, owner.OnComponentChanged);
            }

            public void Invoke() => _operation.Invoke();
        }

        private readonly struct CreateChangeOperationVisitor : IStructVisitor
        {
            private readonly World _world;
            private readonly List<IComponentChangeOperation> _operations;
            private readonly ComponentDependenciesService _owner;

            public CreateChangeOperationVisitor(
                World world,
                List<IComponentChangeOperation> operations,
                ComponentDependenciesService owner)
            {
                _world = world;
                _operations = operations;
                _owner = owner;
            }

            public void Visit<T>(ComponentId componentId) where T : struct
                => _operations.Add(new ChangeOperation<T>(_world, componentId, _owner));
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
