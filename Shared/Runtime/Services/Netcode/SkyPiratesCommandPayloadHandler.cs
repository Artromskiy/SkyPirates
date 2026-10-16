using Delta.Netcode;
using Delta.ECS;
using SlyPirates.Shared.Tools.Json;
using System;
using System.Buffers;

namespace SlyPirates.Shared.Services.Netcode
{
    public sealed class SkyPiratesCommandPayloadHandler : ICommandPayloadHandler
    {
        private readonly World _world;

        public SkyPiratesCommandPayloadHandler(World world) => _world = world;

        public void Write<T>(in T payload, IBufferWriter<byte> output)
            => SerializationUTF8.SerializeCompressed(payload, output);

        public T Read<T>(ReadOnlySpan<byte> payload)
            => SerializationUTF8.DeserializeCompressed<T>(payload.ToArray(), _world);
    }
}
