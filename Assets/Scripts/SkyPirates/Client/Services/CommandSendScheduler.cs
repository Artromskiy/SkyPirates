using DVG.Collections;
using DVG.Commands;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.IServices;

namespace DVG.SkyPirates.Client.Services
{
    public class CommandSendScheduler : ICommandSendScheduler
    {
        private readonly ICommandReciever _commandReciever;
        private readonly ICommandSender _commandSendService;
        private readonly IClientService _client;
        private readonly IPlayer _player;
        private readonly GenericCollection _scheduled = new();

        public CommandSendScheduler(IClientService client, IPlayer player, ICommandSender commandSendService, ICommandReciever commandReciever)
        {
            _client = client;
            _player = player;
            _commandSendService = commandSendService;
            _commandReciever = commandReciever;
        }

        public void SendCommand<T>(Command<T> cmd)
        {
            if (!_client.IsConnected || !_player.CurrentEntityId.HasValue)
                return;

            cmd = cmd.WithClientId(_client.Id);
            _scheduled.Add(cmd);
        }

        public void Tick(int tick)
        {
            var action = new SendCommandAction(tick, _commandSendService, _commandReciever, _scheduled);
            CommandsRegistry.ForEach(ref action);
            _scheduled.Clear();
        }

        private readonly struct SendCommandAction : IGenericAction
        {
            private readonly int _tick;
            private readonly ICommandSender _commandSendService;
            private readonly ICommandReciever _commandReciever;
            private readonly GenericCollection _scheduledCommands;

            public SendCommandAction(int tick, ICommandSender commandSendService, ICommandReciever commandReciever, GenericCollection scheduledCommands)
            {
                _tick = tick;
                _commandSendService = commandSendService;
                _commandReciever = commandReciever;
                _scheduledCommands = scheduledCommands;
            }

            public readonly void Invoke<T>()
            {
                if (!_scheduledCommands.TryGet<Command<T>>(out var cmd))
                    return;

                var cmdMod = cmd.WithTick(_tick);

                if (CommandsRegistry.IsPredicted<T>())
                    _commandReciever.InvokeCommand(cmdMod);

                _commandSendService.SendCommand(cmdMod);
                _scheduledCommands.Remove<Command<T>>();
            }
        }
    }
}
