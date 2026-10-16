using System.IO;
using SlyPirates.Shared.Tools.Json;

namespace SlyPirates.Shared.Services.Replay
{
    public static class SkyPiratesReplayFile
    {
        public static SkyPiratesReplayData Load(string path)
            => SerializationUTF8.Deserialize<SkyPiratesReplayData>(File.ReadAllText(path));

        public static void Save(string path, SkyPiratesReplayData replay)
            => File.WriteAllText(path, SerializationUTF8.Serialize(replay));
    }
}
