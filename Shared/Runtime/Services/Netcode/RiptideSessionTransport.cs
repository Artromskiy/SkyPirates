using Riptide;
using System;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Services.Netcode
{
    internal static class RiptideSessionMessages
    {
        public const ushort MessageId = ushort.MaxValue;
        private const int FragmentHeaderSize = 16;
        private static ushort _nextMessageId;

        public static void Send(ReadOnlySpan<byte> message, Action<Message> send)
        {
            int chunkSize = Math.Max(1, Message.MaxPayloadSize - FragmentHeaderSize);
            if (message.Length <= chunkSize)
            {
                var packet = Message.Create(MessageSendMode.Reliable, MessageId);
                packet.AddBool(false);
                var payload = message.ToArray();
                packet.AddBytes(payload, 0, payload.Length);
                send(packet);
                return;
            }

            int count = (message.Length + chunkSize - 1) / chunkSize;
            if (count > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(message), "The session message exceeds the Riptide fragment limit.");
            }

            ushort id = _nextMessageId++;
            for (int index = 0; index < count; index++)
            {
                int offset = index * chunkSize;
                int length = Math.Min(chunkSize, message.Length - offset);
                var packet = Message.Create(MessageSendMode.Reliable, MessageId);
                packet.AddBool(true);
                packet.AddUShort(id);
                packet.AddUShort((ushort)index);
                packet.AddBool(index == count - 1);
                var payload = message.Slice(offset, length).ToArray();
                packet.AddBytes(payload, 0, payload.Length);
                send(packet);
            }
        }

        public static bool TryRead(Message message, SessionMessageAssembler assembler, ulong connectionId, out byte[] payload)
        {
            payload = null;
            bool fragmented = message.GetBool();
            if (!fragmented)
            {
                int length = checked((int)message.GetVarULong());
                var bytes = new byte[length];
                message.GetBytes(length, bytes);
                payload = bytes;
                return true;
            }

            ushort id = message.GetUShort();
            ushort index = message.GetUShort();
            bool last = message.GetBool();
            int fragmentLength = checked((int)message.GetVarULong());
            var fragment = new byte[fragmentLength];
            message.GetBytes(fragmentLength, fragment);
            return assembler.Add(connectionId, id, index, last, fragment, out payload);
        }
    }

    internal sealed class SessionMessageAssembler
    {
        private readonly Dictionary<(ulong connection, ushort id), PartialMessage> _partial = new();

        public bool Add(ulong connection, ushort id, ushort index, bool isLast, byte[] fragment, out byte[] message)
        {
            var key = (connection, id);
            if (!_partial.TryGetValue(key, out var partial))
            {
                _partial.Add(key, partial = new PartialMessage());
            }

            partial.Fragments[index] = fragment;
            if (isLast)
            {
                partial.LastIndex = index;
            }

            if (!partial.LastIndex.HasValue || partial.Fragments.Count != partial.LastIndex.Value + 1)
            {
                message = null;
                return false;
            }

            int length = 0;
            for (int part = 0; part <= partial.LastIndex.Value; part++)
            {
                if (!partial.Fragments.TryGetValue((ushort)part, out var bytes))
                {
                    message = null;
                    return false;
                }
                length = checked(length + bytes.Length);
            }

            message = new byte[length];
            int offset = 0;
            for (int part = 0; part <= partial.LastIndex.Value; part++)
            {
                var bytes = partial.Fragments[(ushort)part];
                Buffer.BlockCopy(bytes, 0, message, offset, bytes.Length);
                offset += bytes.Length;
            }
            _partial.Remove(key);
            return true;
        }

        private sealed class PartialMessage
        {
            public readonly SortedDictionary<ushort, byte[]> Fragments = new();
            public ushort? LastIndex;
        }
    }

    public sealed class RiptideClientSessionTransport : ISessionTransport
    {
        private readonly Client _client;
        private readonly SessionMessageAssembler _assembler = new();

        public RiptideClientSessionTransport(Client client)
        {
            _client = client;
            _client.MessageReceived += OnMessageReceived;
        }

        public event Action<ulong, byte[]> Received;

        public void Send(ulong connectionId, ReadOnlySpan<byte> message)
            => RiptideSessionMessages.Send(message, packet => _client.Send(packet));

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            if (args.MessageId != RiptideSessionMessages.MessageId)
            {
                return;
            }

            if (RiptideSessionMessages.TryRead(args.Message, _assembler, 0, out var payload))
            {
                Received?.Invoke(0, payload);
            }
        }
    }

    public sealed class RiptideServerSessionTransport : ISessionTransport
    {
        private readonly Server _server;
        private readonly SessionMessageAssembler _assembler = new();

        public RiptideServerSessionTransport(Server server)
        {
            _server = server;
            _server.MessageReceived += OnMessageReceived;
        }

        public event Action<ulong, byte[]> Received;

        public void Send(ulong connectionId, ReadOnlySpan<byte> message)
            => RiptideSessionMessages.Send(message, packet => _server.Send(packet, checked((ushort)connectionId)));

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            if (args.MessageId != RiptideSessionMessages.MessageId)
            {
                return;
            }

            ulong connectionId = args.FromConnection.Id;
            if (RiptideSessionMessages.TryRead(args.Message, _assembler, connectionId, out var payload))
            {
                Received?.Invoke(connectionId, payload);
            }
        }
    }
}
