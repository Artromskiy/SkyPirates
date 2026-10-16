using DVG.SkyPirates.Shared.Core.Collections;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.Services.Netcode;
using System.Collections.Generic;

namespace DVG.SkyPirates.Client.Services
{
    public sealed class CommandSendScheduler : ICommandSendScheduler
    {
        private readonly IClientService _client;
        private readonly IPlayer _player;
        private readonly SkyPiratesSessionProvider _session;
        private readonly Queue<IScheduledCommand> _scheduled = new();
        private readonly GenericCollection _transientInputs = new();

        public CommandSendScheduler(IClientService client, IPlayer player, SkyPiratesSessionProvider session)
        {
            _client = client;
            _player = player;
            _session = session;
        }

        public void SendCommand<T>(T payload)
        {
            if (_session.IsReplay || !_client.IsConnected || !_player.CurrentEntityId.HasValue || !_session.IsReady)
                return;
            _scheduled.Enqueue(new ScheduledCommand<T>(payload));
        }

        public void SetTransientInput<T>(T input)
        {
            _transientInputs.Add(input);
        }

        public bool TryGetTransientInput<T>(out T input)
        {
            return _transientInputs.TryGet(out input);
        }

        public void Tick(int tick)
        {
            if (_session.IsReplay) { _scheduled.Clear(); return; }
            while (_scheduled.Count > 0)
            {
                var command = _scheduled.Peek();
                command.Send(_session, tick);
                _scheduled.Dequeue();
            }
        }

        private interface IScheduledCommand
        {
            void Send(SkyPiratesSessionProvider session, int tick);
        }

        private sealed class ScheduledCommand<T> : IScheduledCommand
        {
            private readonly T _payload;

            public ScheduledCommand(T payload)
            {
                _payload = payload;
            }

            public void Send(SkyPiratesSessionProvider session, int tick) => session.Send(_payload, tick);
        }
    }
}
