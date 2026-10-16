using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace SlyPirates.Shared.Systems
{
    public sealed class HexMapUpdateSystem : IDeltaTickableExecutor
    {
        private readonly Dictionary<Entity, int3> _coordinatesByEntity = new();
        private readonly Dictionary<int3, TileId> _tiles;
        private readonly ComponentChangeOperation<HexTile> _hexTileChanges;
        private readonly ComponentChangeOperation<TileId> _tileIdChanges;
        private readonly ComponentChangeOperation<PendingDestroy> _lifetime;
        private readonly IOperation _updateTiles;
        private readonly IOperation _updateTilesWithoutId;
        private bool _dirty;

        public HexMapUpdateSystem(World world)
        {
            Entity mapEntity = world.Create<HexMap>();
            _tiles = new Dictionary<int3, TileId>();
            world.GetRef<HexMap>(mapEntity).Data = _tiles;
            _hexTileChanges = world.ForEachChange<HexTile>(world.Components.GetPrimary<HexTile>(), OnHexTileChanged);
            _tileIdChanges = world.ForEachChange<TileId>(world.Components.GetPrimary<TileId>(), OnTileIdChanged);
            _lifetime = world.ForEachChange<PendingDestroy>(world.Components.GetPrimary<PendingDestroy>(), OnLifetimeChanged);
            Query tiles = world.WhereAll<HexTile, TileId>().WhereNone<PendingDestroy>();
            _updateTiles = world.ForEachEntity(in tiles, (EntityRef entity, in HexTile tile, in TileId tileId) =>
                SetTile(entity, ToCoordinate(in tile), tileId));
            Query tilesWithoutId = world.WhereAll<HexTile>().WhereNone<TileId, PendingDestroy>();
            _updateTilesWithoutId = world.ForEachEntity(in tilesWithoutId, (EntityRef entity, in HexTile tile) =>
                SetTile(entity, ToCoordinate(in tile), TileId.None));
        }

        public void Tick(int tick, fix deltaTime)
        {
            _hexTileChanges.Invoke();
            _tileIdChanges.Invoke();
            _lifetime.Invoke();
            if (!_dirty) return;
            _updateTiles.Invoke();
            _updateTilesWithoutId.Invoke();
            _dirty = false;
        }

        private void OnHexTileChanged(Entity entity, ChangeKind change, ref HexTile tile)
        {
            if (change == ChangeKind.Removed) RemoveTile(entity);
            else _dirty = true;
        }

        private void OnTileIdChanged(Entity entity, ChangeKind change, ref TileId tileId)
        {
            _dirty = true;
        }

        private void OnLifetimeChanged(Entity entity, ChangeKind change, ref PendingDestroy pending)
        {
            if (change == ChangeKind.Removed) _dirty = true;
            else RemoveTile(entity);
        }

        private void SetTile(Entity entity, int3 coordinate, TileId tileId)
        {
            if (_coordinatesByEntity.TryGetValue(entity, out int3 previousCoordinate))
            {
                if (previousCoordinate != coordinate) _tiles.Remove(previousCoordinate);
                else if (_tiles.TryGetValue(coordinate, out TileId previousId) && previousId == tileId) return;
            }

            _coordinatesByEntity[entity] = coordinate;
            _tiles[coordinate] = tileId;
        }

        private void RemoveTile(Entity entity)
        {
            if (!_coordinatesByEntity.TryGetValue(entity, out int3 coordinate))
                return;

            _coordinatesByEntity.Remove(entity);
            _tiles.Remove(coordinate);
        }

        private static int3 ToCoordinate(in HexTile tile)
            => new(tile.Coordinates.x, tile.Elevation, tile.Coordinates.y);
    }
}
