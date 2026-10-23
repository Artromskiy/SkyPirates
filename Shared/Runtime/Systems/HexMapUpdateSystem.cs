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
        private readonly World _world;
        private readonly Dictionary<Entity, int3> _coordinatesByEntity = new();
        private readonly Dictionary<int3, TileId> _tiles;
        private readonly Dictionary<int2, int> _surfaceElevations;
        private readonly ComponentChangeOperation<HexTile> _hexTileChanges;
        private readonly ComponentChangeOperation<TileId> _tileIdChanges;
        private readonly ComponentChangeOperation<PendingDestroy> _lifetime;

        public HexMapUpdateSystem(World world)
        {
            _world = world;
            Entity mapEntity = world.Create<HexMap>();
            _tiles = new Dictionary<int3, TileId>();
            _surfaceElevations = new Dictionary<int2, int>();
            world.GetRef<HexMap>(mapEntity) = new HexMap
            {
                Data = _tiles,
                SurfaceElevations = _surfaceElevations
            };
            _hexTileChanges = world.ForEachChange<HexTile>(world.Components.GetPrimary<HexTile>(), OnHexTileChanged);
            _tileIdChanges = world.ForEachChange<TileId>(world.Components.GetPrimary<TileId>(), OnTileIdChanged);
            _lifetime = world.ForEachChange<PendingDestroy>(world.Components.GetPrimary<PendingDestroy>(), OnLifetimeChanged);
        }

        public void Tick(int tick, fix deltaTime)
        {
            _hexTileChanges.Invoke();
            _tileIdChanges.Invoke();
            _lifetime.Invoke();
        }

        private void OnHexTileChanged(Entity entity, ChangeKind change, ref HexTile tile)
        {
            if (change == ChangeKind.Removed)
            {
                RemoveTile(entity);
            }
            else if (_world.Has<PendingDestroy>(entity))
            {
                RemoveTile(entity);
            }
            else
            {
                SetTile(entity, ToCoordinate(in tile), GetTileId(entity));
            }
        }

        private void OnTileIdChanged(Entity entity, ChangeKind change, ref TileId tileId)
        {
            if (_world.Has<PendingDestroy>(entity) || !_world.TryGet(entity, out HexTile tile))
            {
                RemoveTile(entity);
                return;
            }

            SetTile(entity, ToCoordinate(in tile), change == ChangeKind.Removed ? TileId.None : tileId);
        }

        private void OnLifetimeChanged(Entity entity, ChangeKind change, ref PendingDestroy pending)
        {
            if (change == ChangeKind.Removed)
            {
                RefreshTile(entity);
            }
            else
            {
                RemoveTile(entity);
            }
        }

        private void RefreshTile(Entity entity)
        {
            if (_world.Has<PendingDestroy>(entity) || !_world.TryGet(entity, out HexTile tile))
            {
                RemoveTile(entity);
                return;
            }

            SetTile(entity, ToCoordinate(in tile), GetTileId(entity));
        }

        private TileId GetTileId(Entity entity)
            => _world.TryGet(entity, out TileId tileId) ? tileId : TileId.None;

        private void SetTile(Entity entity, int3 coordinate, TileId tileId)
        {
            if (_coordinatesByEntity.TryGetValue(entity, out int3 previousCoordinate))
            {
                if (previousCoordinate != coordinate)
                {
                    _tiles.Remove(previousCoordinate);
                }
                else if (_tiles.TryGetValue(coordinate, out TileId previousId) && previousId == tileId)
                {
                    return;
                }
            }

            _coordinatesByEntity[entity] = coordinate;
            _tiles[coordinate] = tileId;
            int2 column = coordinate.xz;
            if (!_surfaceElevations.TryGetValue(column, out int elevation) || coordinate.y > elevation)
            {
                _surfaceElevations[column] = coordinate.y;
            }
        }

        private void RemoveTile(Entity entity)
        {
            if (!_coordinatesByEntity.TryGetValue(entity, out int3 coordinate))
            {
                return;
            }

            _coordinatesByEntity.Remove(entity);
            _tiles.Remove(coordinate);
            int2 column = coordinate.xz;
            if (!_surfaceElevations.TryGetValue(column, out int surface) || surface != coordinate.y)
            {
                return;
            }

            for (int offset = 1; offset <= _tiles.Count; offset++)
            {
                int elevation = surface - offset;
                if (elevation >= surface)
                {
                    break;
                }

                if (_tiles.ContainsKey(new int3(column.x, elevation, column.y)))
                {
                    _surfaceElevations[column] = elevation;
                    return;
                }
            }

            _surfaceElevations.Remove(column);
        }

        private static int3 ToCoordinate(in HexTile tile)
            => new(tile.Coordinates.x, tile.Elevation, tile.Coordinates.y);
    }
}
