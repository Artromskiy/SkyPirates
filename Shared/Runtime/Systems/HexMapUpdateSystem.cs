using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;
using System.Collections.Frozen;

namespace DVG.SkyPirates.Shared.Systems
{
    public sealed class HexMapUpdateSystem : ITransientDeltaTickableExecutor
    {
        private readonly World _world;
        private readonly Dictionary<Entity, int3> _coordinatesByEntity = new();
        private readonly Dictionary<int3, TileId> _tileData = new();
        private readonly ComponentChangeOperation<HexTile> _hexTileChanges;
        private readonly ComponentChangeOperation<TileId> _tileIdChanges;
        private bool _hasChanges;
        private bool _hasPublishedData;

        public HexMapUpdateSystem(World world)
        {
            _world = world;
            _hexTileChanges = world.ForEachChange<HexTile>(world.Components.GetPrimary<HexTile>(), OnHexTileChanged);
            _tileIdChanges = world.ForEachChange<TileId>(world.Components.GetPrimary<TileId>(), OnTileIdChanged);
        }

        public void Tick(int tick, fix deltaTime)
        {
            _hexTileChanges.Invoke();
            _tileIdChanges.Invoke();

            Entity mapEntity = _world.WhereAll<HexMap>().First().Invoke();
            if (!mapEntity.IsValid || (!_hasChanges && _hasPublishedData))
            {
                return;
            }

            _world.GetRef<HexMap>(mapEntity).Data = _tileData.ToFrozenDictionary();
            _hasChanges = false;
            _hasPublishedData = true;
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
            {
                return;
            }

            int3 coordinate = ToCoordinate(_world.Get<HexTile>(entity));
            SetTile(entity, coordinate, change == ChangeKind.Removed ? TileId.None : tileId);
        }

        private void SetTile(Entity entity, int3 coordinate, TileId tileId)
        {
            if (_coordinatesByEntity.TryGetValue(entity, out int3 previousCoordinate) && previousCoordinate != coordinate)
            {
                _tileData.Remove(previousCoordinate);
            }

            _coordinatesByEntity[entity] = coordinate;
            _tileData[coordinate] = tileId;
            _hasChanges = true;
        }

        private void RemoveTile(Entity entity)
        {
            if (!_coordinatesByEntity.TryGetValue(entity, out int3 coordinate))
            {
                return;
            }

            _coordinatesByEntity.Remove(entity);
            _tileData.Remove(coordinate);
            _hasChanges = true;
        }

        private static int3 ToCoordinate(in HexTile tile) =>
            new(tile.Coordinates.x, tile.Elevation, tile.Coordinates.y);

    }
}
