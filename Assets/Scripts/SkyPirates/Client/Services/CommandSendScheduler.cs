using Delta.Netcode;
using DVG.Collections;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Services.Netcode;

namespace DVG.SkyPirates.Client.Services
{
    public sealed class CommandSendScheduler : ICommandSendScheduler
    {
        private readonly IClientService _client;
        private readonly IPlayer _player;
        private readonly SkyPiratesSessionProvider _session;
        private readonly GenericCollection _scheduled = new();

        public CommandSendScheduler(IClientService client, IPlayer player, SkyPiratesSessionProvider session)
        {
            _client = client;
            _player = player;
            _session = session;
        }

        public void SendCommand<T>(T payload)
        {
            if (!_client.IsConnected || !_player.CurrentEntityId.HasValue || !_session.IsReady)
                return;
            _scheduled.Add(payload);
        }

        public void Tick(int tick)
        {
            var visitor = new SendCommands(tick, _session, _scheduled);
            foreach (ICommandRegistration registration in GeneratedCommands.Registrations)
                registration.Visit(ref visitor);
            _scheduled.Clear();
        }

        private readonly struct SendCommands : ICommandVisitor
        {
            private readonly int _tick;
            private readonly SkyPiratesSessionProvider _session;
            private readonly GenericCollection _scheduled;

            public SendCommands(int tick, SkyPiratesSessionProvider session, GenericCollection scheduled)
            {
                _tick = tick;
                _session = session;
                _scheduled = scheduled;
            }

            public void Visit<T>()
            {
                if (!_scheduled.TryGet<T>(out var payload))
                    return;

                _session.Send(payload, _tick);
                _scheduled.Remove<T>();
            }
        }
    }
}
