using Delta.ECS;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DVG.SkyPirates.Shared.Services
{
    public class ComponentDefaultsService : IComponentDefaultsService
    {
        private readonly World _world;
        private readonly ComponentDefaultsConfig _componentDefaultsConfig;
        private readonly IComponentChangeOperation[] _changeOperations;
        private readonly HashSet<Entity> _processedEntities = new();
        private readonly List<Entity> _entitiesToProcess = new();

        public ComponentDefaultsService(World world, ComponentDefaultsConfig componentDefaultsConfig)
        {
            _world = world;
            _componentDefaultsConfig = componentDefaultsConfig;
            _changeOperations = CreateChangeOperations(world, componentDefaultsConfig);
        }

        public void ProcessChanges()
        {
            _processedEntities.Clear();
            _entitiesToProcess.Clear();
            for (int i = 0; i < _changeOperations.Length; i++)
            {
                _changeOperations[i].Invoke();
            }

            for (int i = 0; i < _entitiesToProcess.Count; i++)
            {
                ApplyDefaults(_entitiesToProcess[i]);
            }
        }

        private void OnComponentChanged<T>(Entity entity, ChangeKind change, ref T component) where T : struct
        {
            if (change != ChangeKind.Added || !_processedEntities.Add(entity))
            {
                return;
            }

            _entitiesToProcess.Add(entity);
        }

        private void ApplyDefaults(Entity entity)
        {
            foreach (var item in _componentDefaultsConfig)
            {
                foreach (var entry in item.Default)
                {
                    entry.Visit(_world, new SetDefaultAction(_world, entity));
                }
            }

            foreach (var item in _componentDefaultsConfig)
            {
                foreach (var from in item.CopyFrom)
                {
                    item.CopyFrom.Visit(_world, from, new CopyAction(_world, entity, item.CopyTo));
                }
            }
        }

        private IComponentChangeOperation[] CreateChangeOperations(
            World world,
            ComponentDefaultsConfig config)
        {
            var componentIds = new HashSet<ComponentId>();
            foreach (var item in config)
            {
                foreach (var entry in item.Default)
                {
                    componentIds.Add(world.GetAlias(entry.Name));
                }

                ComponentId[] copyFrom = item.CopyFrom.GetIds(world);
                for (int i = 0; i < copyFrom.Length; i++)
                {
                    componentIds.Add(copyFrom[i]);
                }
            }

            var operations = new List<IComponentChangeOperation>(componentIds.Count);
            var visitor = new CreateChangeOperationVisitor(world, operations, this);
            foreach (ComponentId componentId in componentIds)
            {
                world.Components.Visit(componentId, visitor);
            }

            return operations.ToArray();
        }

        private interface IComponentChangeOperation
        {
            void Invoke();
        }

        private sealed class ChangeOperation<T> : IComponentChangeOperation where T : struct
        {
            private readonly ComponentChangeOperation<T> _operation;

            public ChangeOperation(World world, ComponentId componentId, ComponentDefaultsService owner)
            {
                _operation = world.ForEachChange<T>(componentId, owner.OnComponentChanged);
            }

            public void Invoke() => _operation.Invoke();
        }

        private readonly struct CreateChangeOperationVisitor : IStructComponentVisitor
        {
            private readonly World _world;
            private readonly List<IComponentChangeOperation> _operations;
            private readonly ComponentDefaultsService _owner;

            public CreateChangeOperationVisitor(
                World world,
                List<IComponentChangeOperation> operations,
                ComponentDefaultsService owner)
            {
                _world = world;
                _operations = operations;
                _owner = owner;
            }

            public void Visit<T>(ComponentId componentId) where T : struct
                => _operations.Add(new ChangeOperation<T>(_world, componentId, _owner));
        }

        private readonly struct SetDefaultAction : IStructDataVisitor
        {
            private readonly World _world;
            private readonly Entity _entity;

            public SetDefaultAction(World world, Entity entity)
            {
                _world = world;
                _entity = entity;
            }

            public readonly void Visit<T>(ComponentId componentId, in T value) where T : struct
            {
                if (_world.Has(_entity, componentId))
                {
                    _world.GetRef<T>(_entity, componentId) = value;
                }
            }
        }

        private readonly struct CopyAction : IStructComponentVisitor
        {
            private readonly World _world;
            private readonly Entity _entity;
            private readonly ComponentIdList _to;

            public CopyAction(World world, Entity entity, ComponentIdList to)
            {
                _world = world;
                _entity = entity;
                _to = to;
            }

            public readonly void Visit<From>(ComponentId fromId) where From : struct
            {
                if (_world.TryGet(_entity, out From cmp))
                {
                    foreach (var target in _to)
                    {
                        _to.Visit(_world, target, new SetAction<From>(_world, _entity, cmp));
                    }
                }
            }

            private readonly struct SetAction<From> : IStructComponentVisitor
            {
                private readonly World _world;
                private readonly Entity _entity;
                private readonly From _from;

                public SetAction(World world, Entity entity, From from)
                {
                    _world = world;
                    _entity = entity;
                    _from = from;
                }

                public void Visit<To>(ComponentId toId) where To : struct
                {
                    if (!_world.Has<To>(_entity))
                    {
                        return;
                    }

                    var cmpFrom = _from;
                    var cmpTo = Unsafe.As<From, To>(ref cmpFrom);
                    _world.GetRef<To>(_entity) = cmpTo;
                }
            }
        }
    }
}
