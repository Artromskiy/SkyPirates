using Delta.ECS;
using System.Collections;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Data
{
    public sealed class ComponentDataSet : IReadOnlyList<ComponentDataEntry>
    {
        private readonly List<ComponentDataEntry> _items = new();

        public int Count => _items.Count;
        public ComponentDataEntry this[int index] => _items[index];
        public IEnumerator<ComponentDataEntry> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal void Add(ComponentDataEntry entry) => _items.Add(entry);

        public bool TryGet<T>(out T value) where T : struct
        {
            foreach (var item in _items)
            {
                if (item is ComponentDataEntry<T> typed)
                {
                    value = typed.Value;
                    return true;
                }
            }

            value = default;
            return false;
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
