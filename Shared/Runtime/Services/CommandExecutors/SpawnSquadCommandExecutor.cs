using Delta;
using Delta.ECS;
using Delta.Netcode;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IFactories;
using SlyPirates.Shared.IServices;
using System;
using System.Collections.Generic;

namespace SlyPirates.Shared.Services.CommandExecutors
{
    public class SpawnSquadCommandExecutor : ICommandExecutorRegistration, ICommandExecutor<SpawnSquadCommand>
    {
        private readonly ISquadFactory _squadFactory;
        private readonly IEntityFactory _entityFactory;
        private readonly IComponentDependenciesService _componentDependencies;
        private readonly IComponentDefaultsService _componentDefaults;
        private readonly ShipStatsConfig _shipStats;
        private readonly World _world;
        private readonly Query _spawnPointQuery;
        private readonly List<SpawnLocation> _spawnLocations = new();

        public SpawnSquadCommandExecutor(
            ISquadFactory squadFactory,
            IEntityFactory entityFactory,
            IComponentDependenciesService componentDependencies,
            IComponentDefaultsService componentDefaults,
            ShipStatsConfig shipStats,
            World world)
        {
            _squadFactory = squadFactory;
            _entityFactory = entityFactory;
            _componentDependencies = componentDependencies;
            _componentDefaults = componentDefaults;
            _shipStats = shipStats;
            _world = world;
            _spawnPointQuery = world.WhereAll<SpawnPoint, HexTile, SyncId>().WhereNone<PendingDestroy>();
        }

        public void Execute(in Command<SpawnSquadCommand> cmd)
        {
            TeamId team = (int)cmd.Header.Key.AuthorId.Value;
            var squad = _squadFactory.Create((cmd.Payload.CreationData, team));
            _world.GetRef<Squad>(squad).Deck = cmd.Payload.Deck;
            RandomSeed seed = cmd.Payload.CreationData.RandomSeed;
            SpawnLocation spawn = GetSpawnLocation(ref seed);
            _world.GetRef<Position>(squad) = spawn.Position;

            SyncId shipId = _world.GetRef<SyncIdReserve>(squad).GetNext();
            EntityData stats = _shipStats[0];
            Entity ship = _entityFactory.Create(new EntityParameters(shipId, default, seed), stats.ComponentIds);
            _world.AddOrSet(ship, stats);
            _componentDependencies.Apply(ship);
            _componentDefaults.Apply(ship);
            _world.GetRef<Ship>(ship).SquadId = cmd.Payload.CreationData.SyncId;
            _world.AddOrSet(
                ship,
                new Direction(),
                new Fixation(),
                spawn.ShipPosition,
                new Rotation(),
                team);
        }

        private SpawnLocation GetSpawnLocation(ref RandomSeed seed)
        {
            _spawnLocations.Clear();
            var locations = _spawnLocations;
            var query = _spawnPointQuery;
            _world.ForEach<List<SpawnLocation>, SyncId, HexTile, SpawnPoint>(in query, ref locations,
                static (ref List<SpawnLocation> locations, in SyncId syncId, in HexTile tile, in SpawnPoint spawnPoint) =>
                {
                    var coordinates = new int3(tile.Coordinates.x, tile.Elevation, tile.Coordinates.y);
                    locations.Add(new SpawnLocation(syncId.Value,
                        new Position { Value = Hex.AxialToWorld(coordinates) },
                        new Position { Value = Hex.AxialToWorld(spawnPoint.ShipCoordinates).x_y }));
                }).Invoke(ref locations);
            _spawnLocations.Sort(static (left, right) => left.SyncId.CompareTo(right.SyncId));

            if (_spawnLocations.Count == 0)
            {
                throw new InvalidOperationException("Map data must contain at least one coastal spawn point.");
            }

            return _spawnLocations[seed.NextRange(0, _spawnLocations.Count)];
        }

        private readonly struct SpawnLocation
        {
            public readonly int SyncId;
            public readonly Position Position;
            public readonly Position ShipPosition;

            public SpawnLocation(int syncId, Position position, Position shipPosition)
            {
                SyncId = syncId;
                Position = position;
                ShipPosition = shipPosition;
            }
        }
    }
}
