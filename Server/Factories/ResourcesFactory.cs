using SlyPirates.Shared.Core;
using SlyPirates.Shared;
using SlyPirates.Shared.Tools.Json;
using Delta.ECS;
using System.IO;

namespace SlyPirates.Server.Factories
{
    public class ResourcesFactory<T> : IPathFactory<T>
    {
        private readonly World _world;

        public ResourcesFactory(World world) => _world = world;

        public T Create(string parameters)
        {
            var path = Path.Combine("Resources", parameters);
            var text = File.ReadAllText(path + ".json");
            return SerializationUTF8.Deserialize<T>(text, _world);
        }
    }
}
