using Delta.Netcode;
using System;

namespace DVG.SkyPirates.Shared.Services.Netcode
{
    public interface ISessionTransport : ITransport
    {
        event Action<ulong, byte[]> Received;
    }

    public sealed class NullSessionTransport : ISessionTransport
    {
        public event Action<ulong, byte[]> Received { add { } remove { } }
        public void Send(ulong connectionId, ReadOnlySpan<byte> message) { }
    }
}
