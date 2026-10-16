using Delta.ECS;
using System.Collections;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Data
{
    public sealed class ComponentIdList : IReadOnlyList<string>
    {
        private readonly List<string> _items = new();
        public int Count => _items.Count;
        public string this[int index] => _items[index];
        public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        internal void Add(string name) => _items.Add(name);
        internal void Visit(World world, string name, IStructComponentVisitor visitor)
        {
            ComponentId componentId = world.GetAlias(name);
            world.Components.Visit(componentId, visitor);
        }
        public ComponentId[] GetIds(World world)
        {
            var ids = new ComponentId[_items.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = world.GetAlias(_items[i]);
            return ids;
        }
    }

}
