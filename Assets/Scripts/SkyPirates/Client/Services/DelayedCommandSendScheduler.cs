using DVG.Collections;
using DVG.Commands;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.IServices;
using System;
using System.Collections.Generic;

namespace DVG.SkyPirates.Client.Services
{
    public class DelayedCommandSendScheduler : ICommandSendScheduler
    {
        private const int MaxDelayFrames = 4;
        private const int MinDelayFrames = 0;
        private const double DelayChanse = 1;

        private readonly ICommandReciever _commandReciever;
        private readonly ICommandSender _sendService;
        private readonly IClientService _client;

        private readonly GenericCollection _scheduled = new();
        private readonly GenericCollection _delayed = new();
        private readonly Random _random = new();

        public DelayedCommandSendScheduler(IClientService client, ICommandSender commandSendService, ICommandReciever commandReciever)
        {
            _client = client;
            _sendService = commandSendService;
            _commandReciever = commandReciever;
        }

        public void SendCommand<T>(Command<T> cmd)
        {
            if (!_client.IsConnected)
                return;

            cmd = cmd.WithClientId(_client.Id);

            _scheduled.Add(cmd);
        }

        public void Tick(int tick)
        {
            ProcessScheduledCommands schedule = new(tick, _random, _scheduled, _delayed, _commandReciever);
            CommandsRegistry.ForEach(ref schedule);
            SendDelayedCommands delayed = new(tick, _sendService, _delayed);
            CommandsRegistry.ForEach(ref delayed);
        }

        private readonly struct ProcessScheduledCommands : IGenericAction
        {
            private readonly int _tick;
            private readonly Random _random;
            private readonly GenericCollection _scheduled;
            private readonly GenericCollection _delayed;
            private readonly ICommandReciever _commandReciever;

            public ProcessScheduledCommands(int tick, Random random, GenericCollection scheduled, GenericCollection delayed, ICommandReciever commandReciever)
            {
                _tick = tick;
                _random = random;
                _scheduled = scheduled;
                _delayed = delayed;
                _commandReciever = commandReciever;
            }

            public void Invoke<T>()
            {
                if (!_scheduled.TryGet<Command<T>>(out var command))
                    return;

                _scheduled.Remove<Command<T>>();
                var updated = command.WithTick(_tick);

                if (CommandsRegistry.IsPredicted<T>())
                    _commandReciever.InvokeCommand(updated);

                if (!_delayed.TryGet<Queue<(Command<T> cmd, int sendTick)>>(out var cmdList))
                    _delayed.Add(cmdList = new());

                var next = _random.NextDouble();
                var delay = DelayChanse > next ? _random.Next(MinDelayFrames, MaxDelayFrames) : 0;
                cmdList.Enqueue((updated, _tick + delay));
            }
        }
        private readonly struct SendDelayedCommands : IGenericAction
        {
            private readonly int _tick;
            private readonly ICommandSender _send;
            private readonly GenericCollection _delayed;

            public SendDelayedCommands(int tick, ICommandSender send, GenericCollection delayed)
            {
                _tick = tick;
                _send = send;
                _delayed = delayed;
            }

            public void Invoke<T>()
            {
                if (!_delayed.TryGet<Queue<(Command<T> cmd, int sendTick)>>(out var commands))
                    return;

                int count = commands.Count;
                for (int i = 0; i < count; i++)
                {
                    var cmdData = commands.Dequeue();
                    if (cmdData.sendTick >= _tick)
                        _send.SendCommand(cmdData.cmd);
                    else
                        commands.Enqueue(cmdData);
                }
            }
        }
    }
}
