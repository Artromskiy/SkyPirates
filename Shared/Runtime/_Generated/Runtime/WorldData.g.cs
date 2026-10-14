using System;
using System.Collections.Generic;
using Delta.ECS;

namespace DVG.SkyPirates.Shared.Data
{
    public sealed class WorldData
    {
        private readonly Dictionary<Type, IWorldDataComponent> _components = new();
        private readonly List<IWorldDataComponent> _orderedComponents = new();

        public Dictionary<int, T> Get<T>() where T : struct
        {
            if (_components.TryGetValue(typeof(T), out IWorldDataComponent component))
                return ((WorldDataComponent<T>)component).Values;

            var created = new WorldDataComponent<T>();
            _components.Add(typeof(T), created);
            _orderedComponents.Add(created);
            return created.Values;
        }

        public void Set<T>(Dictionary<int, T> data) where T : struct
        {
            if (_components.TryGetValue(typeof(T), out IWorldDataComponent component))
            {
                ((WorldDataComponent<T>)component).Values = data;
                return;
            }

            var created = new WorldDataComponent<T>(data);
            _components.Add(typeof(T), created);
            _orderedComponents.Add(created);
        }

        public void TrimExcess()
        {
            for (int i = 0; i < _orderedComponents.Count; i++)
                _orderedComponents[i].TrimExcess();
        }

        internal IReadOnlyList<IWorldDataComponent> Components => _orderedComponents;
    }

    internal interface IWorldDataComponent
    {
        string Name { get; }
        Type DataType { get; }
        object Data { get; }
        void TrimExcess();
    }

    internal sealed class WorldDataComponent<T> : IWorldDataComponent where T : struct
    {
        private static readonly string ComponentName = GetComponentName();

        public Dictionary<int, T> Values { get; set; }
        public string Name => ComponentName;
        internal static string NameForType => ComponentName;
        public Type DataType => typeof(Dictionary<int, T>);
        public object Data => Values;

        public WorldDataComponent() : this(new Dictionary<int, T>()) { }

        public WorldDataComponent(Dictionary<int, T> values) => Values = values;

        public void TrimExcess() => Values.TrimExcess();

        private static string GetComponentName()
        {
            object[] attributes = typeof(T).GetCustomAttributes(typeof(ComponentAliasAttribute), false);
            return attributes.Length == 0
                ? typeof(T).Name
                : ((ComponentAliasAttribute)attributes[0]).Alias;
        }
    }
}
