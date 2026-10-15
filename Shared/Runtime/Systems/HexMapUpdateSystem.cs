using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Systems
{
    public sealed class HexMapUpdateSystem : IDeltaTickableExecutor
    {
        private readonly World _world;
        private readonly Entity _mapEntity;
        private readonly Dictionary<Entity, int3> _coordinatesByEntity = new();
        private readonly ComponentChangeOperation<HexTile> _hexTileChanges;
        private readonly ComponentChangeOperation<TileId> _tileIdChanges;

        public HexMapUpdateSystem(World world)
        {
            _world = world;
            _mapEntity = world.Create<HexMap>();
            _world.GetRef<HexMap>(_mapEntity).Data = new Dictionary<int3, TileId>();
            _hexTileChanges = world.ForEachChange<HexTile>(world.Components.GetPrimary<HexTile>(), OnHexTileChanged);
            _tileIdChanges = world.ForEachChange<TileId>(world.Components.GetPrimary<TileId>(), OnTileIdChanged);
        }

        public void Tick(int tick, fix deltaTime)
        {
            _hexTileChanges.Invoke();
            _tileIdChanges.Invoke();
        }

        private void OnHexTileChanged(Entity entity, ChangeKind change, ref HexTile tile)
        {
            if (change == ChangeKind.Removed)
            {
                RemoveTile(entity);
                return;
            }

            int3 coordinate = ToCoordinate(tile);
            TileId tileId = _world.Has<TileId>(entity) ? _world.Get<TileId>(entity) : TileId.None;
            SetTile(entity, coordinate, tileId);
        }

        private void OnTileIdChanged(Entity entity, ChangeKind change, ref TileId tileId)
        {
            if (!_world.Has<HexTile>(entity))
                return;

            int3 coordinate = ToCoordinate(_world.Get<HexTile>(entity));
            SetTile(entity, coordinate, change == ChangeKind.Removed ? TileId.None : tileId);
        }

        private void SetTile(Entity entity, int3 coordinate, TileId tileId)
        {
            ref HexMap map = ref _world.GetRef<HexMap>(_mapEntity);
            if (_coordinatesByEntity.TryGetValue(entity, out int3 previousCoordinate) && previousCoordinate != coordinate)
                map.Data.Remove(previousCoordinate);

            _coordinatesByEntity[entity] = coordinate;
            map.Data[coordinate] = tileId;
        }

        private void RemoveTile(Entity entity)
        {
            if (!_coordinatesByEntity.TryGetValue(entity, out int3 coordinate))
                return;

            _coordinatesByEntity.Remove(entity);
            _world.GetRef<HexMap>(_mapEntity).Data.Remove(coordinate);
        }

        private static int3 ToCoordinate(in HexTile tile)
            => new(tile.Coordinates.x, tile.Elevation, tile.Coordinates.y);
    }
}
