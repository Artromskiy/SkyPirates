using DVG.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace DVG.SkyPirates.OldShared.Factories
{
    public class ResourcesFactory<T> : IPathFactory<T>
    {
        public T Create(string parameters)
        {
            var textAsset = Resources.Load<TextAsset>(parameters);
            var text = textAsset.text;
            return JsonConvert.DeserializeObject<T>(text);
        }
    }
}