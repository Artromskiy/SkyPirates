#nullable enable
using DVG.Commands;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.Services;
using Riptide;
using System.Collections.Generic;

namespace DVG.SkyPirates.Client.Services
{
    internal class CommandSendService : ICommandSender
    {
        private readonly Riptide.Client _client;
        private readonly MessageIO _messageIO;
        private readonly List<Message> _messages = new();

        public CommandSendService(Riptide.Client client)
        {
            _messageIO = new();
            _client = client;
        }

        public void SendCommand<T>(Command<T> cmd)
        {
            _messages.Clear();
            _messageIO.GetMessages(cmd, _messages);
            foreach (var message in _messages)
            {
                _client.Send(message);
            }
        }
    }
}
