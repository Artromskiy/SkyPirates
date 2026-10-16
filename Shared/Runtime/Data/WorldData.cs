using System;
using System.Collections.Generic;

namespace SlyPirates.Shared.Data
{
    public sealed class WorldData
    {
        private readonly Dictionary<Type, object> _components = new();

        public Dictionary<int, T> Get<T>() where T : struct
        {
            Type componentType = typeof(T);
            if (!_components.TryGetValue(componentType, out object? data))
            {
                data = new Dictionary<int, T>();
                _components.Add(componentType, data);
            }

            return (Dictionary<int, T>)data;
        }

        public void Set<T>(Dictionary<int, T> data) where T : struct
            => _components[typeof(T)] = data;

        internal IEnumerable<KeyValuePair<Type, object>> Components => _components;

        internal void Set(Type componentType, object data)
            => _components[componentType] = data;
    }
}
