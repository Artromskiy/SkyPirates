using SlyPirates.Shared.Core.Ids;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IFactories;
using Delta.ECS;
using System;
using System.Collections.Generic;

namespace SlyPirates.Shared.Factories
{
    public class EntityConfigFactory<T> : IEntityConfigFactory<T> where T : struct, IId, IEquatable<T>
    {
        private readonly Dictionary<T, ComponentDataSet> _entities = new();
        private readonly ComponentId _keyComponentId;

        public EntityConfigFactory(GlobalConfig config, World world)
        {
            _keyComponentId = world.Components.GetPrimary<T>();

            foreach (var item in config.UnitsStats)
            {
                TryAdd(item.Value, world);
            }

            foreach (var item in config.CactusesStats)
            {
                TryAdd(item.Value, world);
            }

            foreach (var item in config.TreesStats)
            {
                TryAdd(item.Value, world);
            }

            foreach (var item in config.RocksStats)
            {
                TryAdd(item.Value, world);
            }

            foreach (var item in config.GoodsStats)
            {
                TryAdd(item.Value, world);
            }
        }

        private void TryAdd(ComponentDataSet data, World world)
        {
            if (data.TryGet(world, _keyComponentId, out T key))
            {
                _entities.Add(key, data);
            }
        }

        public ComponentDataSet Create(T parameters)
        {
            _entities.TryGetValue(parameters, out var entityData);
            return entityData;
        }
    }
}
