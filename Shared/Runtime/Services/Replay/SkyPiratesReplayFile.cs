using Delta.ECS;
using System.IO;
using SlyPirates.Shared.Tools.Json;

namespace SlyPirates.Shared.Services.Replay
{
    public static class SkyPiratesReplayFile
    {
        public static SkyPiratesReplayData Load(string path, World world)
            => SerializationUTF8.Deserialize<SkyPiratesReplayData>(File.ReadAllText(path), world);

        public static void Save(string path, SkyPiratesReplayData replay, World world)
            => File.WriteAllText(path, SerializationUTF8.Serialize(replay, world));
    }
}
