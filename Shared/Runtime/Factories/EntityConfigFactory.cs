using DVG.Ids;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IFactories;
using System;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Factories
{
    public class EntityConfigFactory<T> : IEntityConfigFactory<T> where T : struct, IId, IEquatable<T>
    {
        private readonly Dictionary<T, ComponentDataSet> _entities = new();

        public EntityConfigFactory(GlobalConfig config)
        {
            foreach (var item in config.UnitsStats)
            {
                TryAdd(item.Value);
            }

            foreach (var item in config.CactusesStats)
            {
                TryAdd(item.Value);
            }

            foreach (var item in config.TreesStats)
            {
                TryAdd(item.Value);
            }

            foreach (var item in config.RocksStats)
            {
                TryAdd(item.Value);
            }

            foreach (var item in config.GoodsStats)
            {
                TryAdd(item.Value);
            }
        }

        private void TryAdd(ComponentDataSet data)
        {
            if (data.TryGet<T>(out var key))
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
