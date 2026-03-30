using DVG.SkyPirates.Shared.Tools.Json;
using NaughtyAttributes;
using System.IO;
using UnityEngine;
using HashedTimeline = System.ValueTuple<System.Collections.Generic.Dictionary<int, DVG.SkyPirates.Shared.Data.WorldData>, DVG.SkyPirates.Shared.Commands.CommandsData>;

namespace DVG.SkyPirates.Tooling.Testing
{
    public class SerializeChecker : MonoBehaviour
    {
        [SerializeField]
        private TextAsset _textAsset;

        private void Update()
        {
            TestSerialize();
        }

        [Button]
        private void TestSerialize()
        {
            var commandsData = SerializationUTF8.
                DeserializeCompressed<HashedTimeline>(_textAsset.bytes).Item2;

            var commandsDataJson = SerializationUTF8.SerializeOrdered(commandsData);
            File.WriteAllText(GetPath("CommandsData"), commandsDataJson);
        }

        private string GetPath(string fileName)
        {
            const string folder = "Scripts/SkyPirates/Tooling/TimelineDebug";
            var path = Path.Combine(Application.dataPath, folder, fileName);
            path = Path.ChangeExtension(path, "json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return path;
        }
    }
}