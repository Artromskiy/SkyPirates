#nullable enable
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.IServices;
using Riptide;
using System;

namespace DVG.SkyPirates.Client.Services
{
    internal class CommandSendService : ICommandSendService
    {
        private readonly Riptide.Client _client;
        private byte[] _tempBytes = Array.Empty<byte>();
        private readonly ICommandSerializer _commandSerializer;

        public CommandSendService(Riptide.Client client, ICommandSerializer commandSerializer)
        {
            _client = client;
            _commandSerializer = commandSerializer;
        }

        public void SendCommand<T>(T data) where T : unmanaged
        {
            var message = CreateMessage(data);
            _client.Send(message);
        }

        private Message CreateMessage<T>(T data) where T : unmanaged
        {
            var span = _commandSerializer.Serialize(ref data);
            int length = span.Length;
            if (length > _tempBytes.Length)
                Array.Resize(ref _tempBytes, length);
            span.CopyTo(_tempBytes);

            int id = CommandIds.GetId<T>();
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)id);
            message.AddBytes(_tempBytes, 0, length);
            return message;
        }
    }
}
