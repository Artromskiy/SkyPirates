using Delta.ECS;
using SlyPirates.Shared.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SlyPirates.Shared.Data
{
    public sealed class ComponentDataSet : IReadOnlyList<ComponentDataEntry>
    {
        private readonly List<ComponentDataEntry> _items = new();
        private readonly ConditionalWeakTable<World, Dictionary<ComponentId, ComponentDataEntry>> _itemsByWorld = new();

        public int Count => _items.Count;
        public ComponentDataEntry this[int index] => _items[index];
        public IEnumerator<ComponentDataEntry> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static ComponentDataSet Capture(World world, Entity entity, ReadOnlySpan<ComponentId> excluded)
        {
            var result = new ComponentDataSet();
            ReadOnlySpan<ComponentId> componentIds = world.GetGroup(Group.All);
            var visitor = new CaptureComponentData(world, entity, result);
            for (int i = 0; i < componentIds.Length; i++)
            {
                ComponentId componentId = componentIds[i];
                bool shouldExclude = false;
                for (int j = 0; j < excluded.Length; j++)
                {
                    if (excluded[j] == componentId)
                    {
                        shouldExclude = true;
                        break;
                    }
                }

                if (!shouldExclude && world.Has(entity, componentId))
                {
                    world.Components.Visit(componentId, visitor);
                }
            }

            return result;
        }

        internal ComponentId[] GetComponentIds(World world)
        {
            var componentIds = new ComponentId[_items.Count];
            for (int i = 0; i < _items.Count; i++)
            {
                componentIds[i] = world.GetAlias(_items[i].Name);
            }

            return componentIds;
        }

        internal void Add(World world, ComponentId componentId, ComponentDataEntry entry)
        {
            _items.Add(entry);
            Dictionary<ComponentId, ComponentDataEntry> itemsById = _itemsByWorld.GetValue(world, static _ => new());
            itemsById[componentId] = entry;
        }

        public bool TryGet<T>(World world, ComponentId componentId, out T value) where T : struct
        {
            Dictionary<ComponentId, ComponentDataEntry> itemsById = _itemsByWorld.GetValue(world, static _ => new());
            if (itemsById.TryGetValue(componentId, out var item) && item is ComponentDataEntry<T> typed)
            {
                value = typed.Value;
                return true;
            }

            value = default;
            return false;
        }

        private sealed class CaptureComponentData : IStructComponentVisitor
        {
            private readonly World _world;
            private readonly Entity _entity;
            private readonly ComponentDataSet _data;

            public CaptureComponentData(World world, Entity entity, ComponentDataSet data)
            {
                _world = world;
                _entity = entity;
                _data = data;
            }

            public void Visit<T>(ComponentId componentId) where T : struct
            {
                T value = _world.GetReadRef<T>(_entity, componentId);
                object[] attributes = typeof(T).GetCustomAttributes(typeof(ComponentAliasAttribute), false);
                string name = attributes.Length == 0
                    ? typeof(T).Name
                    : ((ComponentAliasAttribute)attributes[0]).Alias;
                _data.Add(_world, componentId, new ComponentDataEntry<T>(name, in value));
            }
        }
    }

    public abstract class ComponentDataEntry
    {
        public string Name { get; }

        protected ComponentDataEntry(string name) => Name = name;

        public abstract void Write(IComponentDataWriter writer);
        internal abstract void Visit(World world, IStructDataVisitor visitor);
    }

    public sealed class ComponentDataEntry<T> : ComponentDataEntry where T : struct
    {
        public T Value { get; }

        public ComponentDataEntry(string name, in T value) : base(name) => Value = value;

        public override void Write(IComponentDataWriter writer)
        {
            var value = Value;
            writer.Write(Name, in value);
        }

        internal override void Visit(World world, IStructDataVisitor visitor)
        {
            ComponentId componentId = world.GetAlias(Name);
            var value = Value;
            visitor.Visit(componentId, in value);
        }
    }

    public interface IComponentDataWriter
    {
        void Write<T>(string name, in T value) where T : struct;
    }

    internal interface IStructDataVisitor
    {
        void Visit<T>(ComponentId componentId, in T value) where T : struct;
    }
}
