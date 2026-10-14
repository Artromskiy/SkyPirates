using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IFactories;
using System.Collections.Frozen;

namespace DVG.SkyPirates.Shared.Factories
{
    public class HexMapFactory : IHexMapFactory
    {
        private readonly IEntityFactory _commandEntityFactory;
        private readonly World _world;

        public HexMapFactory(World world, IEntityFactory commandEntityFactory)
        {
            _world = world;
            _commandEntityFactory = commandEntityFactory;
        }

        public Entity Create(int parameters)
        {
            EntityParameters entityParameters = new()
            {
                SyncId = parameters,
                RandomSeed = default,
                SyncIdReserve = default,
            };
            var entity = _commandEntityFactory.Create(entityParameters);
            _world.Add<HexMap>(entity, new() { Data = FrozenDictionary<int3, Ids.TileId>.Empty });
            return entity;
        }
    }
}
