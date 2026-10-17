using Delta;
using Delta.ECS;
using Delta.Netcode;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.IFactories;
using SlyPirates.Shared.IServices;
using System;
using System.Collections.Generic;

namespace SlyPirates.Shared.Services.CommandExecutors
{
    public class SpawnSquadCommandExecutor : ICommandExecutorRegistration, ICommandExecutor<SpawnSquadCommand>
    {
        private readonly ISquadFactory _squadFactory;
        private readonly World _world;
        private readonly Query _spawnPointQuery;
        private readonly List<SpawnLocation> _spawnLocations = new();
        private bool _spawnLocationsLoaded;

        public SpawnSquadCommandExecutor(
            ISquadFactory squadFactory,
            World world)
        {
            _squadFactory = squadFactory;
            _world = world;
            _spawnPointQuery = world.WhereAll<SpawnPoint, HexTile, SyncId>().WhereNone<PendingDestroy>();
        }

        public void Execute(in Command<SpawnSquadCommand> cmd)
        {
            TeamId team = (int)cmd.Header.Key.AuthorId.Value;
            var squad = _squadFactory.Create((cmd.Payload.CreationData, team));
            _world.GetRef<Squad>(squad).Deck = cmd.Payload.Deck;
            RandomSeed seed = cmd.Payload.CreationData.RandomSeed;
            _world.GetRef<Position>(squad) = GetSpawnPosition(ref seed);
        }

        private Position GetSpawnPosition(ref RandomSeed seed)
        {
            if (!_spawnLocationsLoaded)
            {
                var locations = _spawnLocations;
                var query = _spawnPointQuery;
                _world.ForEach<List<SpawnLocation>, SyncId, HexTile>(in query, ref locations,
                    static (ref List<SpawnLocation> result, in SyncId syncId, in HexTile tile) =>
                    {
                        var coordinates = new int3(tile.Coordinates.x, tile.Elevation, tile.Coordinates.y);
                        result.Add(new SpawnLocation(syncId.Value, new Position { Value = Hex.AxialToWorld(coordinates) }));
                    }).Invoke(ref locations);
                _spawnLocations.Sort(static (left, right) => left.SyncId.CompareTo(right.SyncId));
                _spawnLocationsLoaded = true;
            }

            if (_spawnLocations.Count == 0)
            {
                throw new InvalidOperationException("Map data must contain at least one spawn point.");
            }

            return _spawnLocations[seed.NextRange(0, _spawnLocations.Count)].Position;
        }

        private readonly struct SpawnLocation
        {
            public readonly int SyncId;
            public readonly Position Position;

            public SpawnLocation(int syncId, Position position)
            {
                SyncId = syncId;
                Position = position;
            }
        }
    }
}
