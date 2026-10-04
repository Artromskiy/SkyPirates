using Delta.Netcode;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.IServices;

namespace DVG.SkyPirates.Client.Services
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
            if (SkyPiratesCommand.GetClientId(cmd) != _client.Id)
                return;

            SquadEntityId = cmd.Payload.CreationData.SyncId;
            CurrentEntityId = cmd.Payload.CreationData.SyncId;
        }
    }
}
