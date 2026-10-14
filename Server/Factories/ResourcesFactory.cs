using DVG.Core;
using DVG.SkyPirates.Shared.Tools.Json;
using System.IO;

namespace DVG.SkyPirates.Server.Factories
{
    public class ResourcesFactory<T> : IPathFactory<T>
    {
        public T Create(string parameters)
        {
            var path = Path.Combine("Resources", parameters);
            var text = File.ReadAllText(path + ".json");
            return SerializationUTF8.Deserialize<T>(text);
        }
    }
}
