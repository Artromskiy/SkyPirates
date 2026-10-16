using Delta.ECS;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IServices;
using System.Runtime.CompilerServices;

namespace SlyPirates.Shared.Services
{
    public class ComponentDefaultsService : IComponentDefaultsService
    {
        private readonly World _world;
        private readonly ComponentDefaultsConfig _componentDefaultsConfig;

        public ComponentDefaultsService(World world, ComponentDefaultsConfig componentDefaultsConfig)
        {
            _world = world;
            _componentDefaultsConfig = componentDefaultsConfig;
        }

        public void Apply(Entity entity)
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
