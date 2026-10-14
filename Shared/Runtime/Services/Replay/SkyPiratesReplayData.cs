using DVG.SkyPirates.Shared.Data;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Services.Replay
{
    public sealed class SkyPiratesReplayData
    {
        public WorldData InitialWorld = null!;
        public long InitialStep;
        public List<SkyPiratesReplayCommand> Commands = new();
        public SkyPiratesReplayHashCheck EndHashCheck = null!;
    }

    public sealed class SkyPiratesReplayCommand
    {
        public uint AuthorId;
        public ulong Sequence;
        public ulong TypeId;
        public long Step;
        public uint Order;
        public byte[] Payload = System.Array.Empty<byte>();
    }

    public sealed class SkyPiratesReplayHashCheck
    {
        public long Step;
        public ulong Hash;
    }
}
