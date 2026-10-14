using Delta.Netcode;
using DVG.SkyPirates.Shared.Tools.Json;
using System;
using System.Buffers;

namespace DVG.SkyPirates.Shared.Services.Netcode
{
    public sealed class SkyPiratesCommandPayloadHandler : ICommandPayloadHandler
    {
        public void Write<T>(in T payload, IBufferWriter<byte> output)
            => SerializationUTF8.SerializeCompressed(payload, output);

        public T Read<T>(ReadOnlySpan<byte> payload)
            => SerializationUTF8.DeserializeCompressed<T>(payload.ToArray());
    }
}
