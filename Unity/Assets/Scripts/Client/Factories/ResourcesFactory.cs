#nullable enable
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Tools.Json;
using Delta.ECS;
using UnityEngine;

namespace SlyPirates.Client.Factories
{
    public class ResourcesFactory<T> : IPathFactory<T>
    {
        private readonly World _world;

        public ResourcesFactory(World world) => _world = world;

        public T Create(string parameters)
        {
            var textAsset = Resources.Load<TextAsset>(parameters);
            return SerializationUTF8.Deserialize<T>(textAsset.bytes, _world);
        }
    }
}
