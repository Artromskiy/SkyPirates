#nullable enable
using DVG.SkyPirates.Shared.Core;
using DVG.SkyPirates.Shared;
using DVG.SkyPirates.Shared.Tools.Json;
using Delta.ECS;
using UnityEngine;

namespace DVG.SkyPirates.Client.Factories
{
    public class ResourcesFactory<T> : IPathFactory<T>
    {
        private readonly World _world;

        public ResourcesFactory(World world) => _world = world;

        public T Create(string parameters)
        {
            var textAsset = Resources.Load<TextAsset>(parameters);
            string text = textAsset.text;
            return SerializationUTF8.Deserialize<T>(text, _world);
        }
    }
}
