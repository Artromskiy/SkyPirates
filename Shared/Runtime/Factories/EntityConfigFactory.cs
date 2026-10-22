using SlyPirates.Shared.Core.Ids;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IFactories;
using System;
using System.Collections.Generic;
using Delta.ECS;

namespace SlyPirates.Shared.Factories
{
    public sealed class EntityConfigFactory : IEntityConfigFactory
    {
        private readonly Dictionary<string, EntityData> _entities = new(StringComparer.Ordinal);

        public EntityConfigFactory(GlobalConfig config)
        {
            Add(config.UnitsStats);
            Add(config.CactusesStats);
            Add(config.TreesStats);
            Add(config.RocksStats);
            Add(config.GoodsStats);
        }

        private void Add<TId>(IEnumerable<KeyValuePair<TId, EntityData>> entities)
            where TId : struct, IId, IEquatable<TId>
        {
            foreach (var item in entities)
            {
                _entities.Add(item.Key.Value, item.Value);
            }
        }

        public EntityData Create(string id) => _entities[id];
    }
}
