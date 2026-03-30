#nullable enable
using DVG.Core;
using DVG.SkyPirates.Shared.Tools.Json;
using UnityEngine;

namespace DVG.SkyPirates.Client.Factories
{
    public class ResourcesFactory<T> : IPathFactory<T>
    {
        public T Create(string parameters)
        {
            var textAsset = Resources.Load<TextAsset>(parameters);
            var text = textAsset.text;
            return SerializationUTF8.Deserialize<T>(text);
        }
    }
}