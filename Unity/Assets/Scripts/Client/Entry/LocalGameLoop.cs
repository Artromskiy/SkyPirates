using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Services.Netcode;

namespace SlyPirates.Client.Entry
{
    public sealed class LocalGameLoop
    {
        private readonly SkyPiratesSessionProvider _session;
        private readonly GameStartController _gameStart;
        private bool _spawnSquadSent;

        public bool Enabled { get; set; } = true;

        public LocalGameLoop(SkyPiratesSessionProvider session, GameStartController gameStart)
        {
            _session = session;
            _gameStart = gameStart;
        }

        public void Update()
        {
            if (!Enabled)
            {
                return;
            }

            _gameStart.Update();
            if (_spawnSquadSent || _session.CurrentStep < 0)
            {
                return;
            }

            Delta.Diagnostics.Trace.Info("[LocalStart] Spawn squad");
            _session.Send(new SpawnSquadCommand { Deck = ClientSetupData.StartingDeck }, _session.CurrentStep + 1);
            _spawnSquadSent = true;
        }
    }
}
