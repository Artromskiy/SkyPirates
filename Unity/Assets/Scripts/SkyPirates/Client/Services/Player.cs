using Delta.Netcode;
using SkyPirates.Client.IServices;
using SkyPirates.Shared.Commands;
using SkyPirates.Shared.IServices;

namespace SkyPirates.Client.Services
{
    public class Player : IPlayer
    {
        public int? SquadEntityId { get; private set; }
        public int? ShipEntityId { get; private set; }
        public int? CurrentEntityId { get; private set; }

        private readonly IClientService _client;
        private readonly ICommandReciever _comandReciever;

        public Player(IClientService client, ICommandReciever comandReciever)
        {
            _client = client;
            _comandReciever = comandReciever;

            _comandReciever.RegisterReciever<SpawnSquadCommand>(OnSpawnSquad);
        }

        private void OnSpawnSquad(Command<SpawnSquadCommand> cmd)
        {
            if ((int)cmd.Header.Key.AuthorId.Value != _client.Id)
                return;

            SquadEntityId = cmd.Payload.CreationData.SyncId;
            CurrentEntityId = cmd.Payload.CreationData.SyncId;
        }
    }
}
