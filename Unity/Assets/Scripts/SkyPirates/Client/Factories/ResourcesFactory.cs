#nullable enable
using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Shared.Tools.Json;
using UnityEngine;

namespace SkyPirates.Client.Factories
{
    public class ResourcesFactory<T> : IPathFactory<T>
    {
        public T Create(string parameters)
        {
            var textAsset = Resources.Load<TextAsset>(parameters);
            string text = textAsset.text;
            return SerializationUTF8.Deserialize<T>(text);
        }
    }
}