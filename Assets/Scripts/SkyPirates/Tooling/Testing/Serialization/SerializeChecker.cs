using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Tools.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

        [ContextMenu("Serialization/Test Serialize")]
        private void TestSerialize()
        {
            var commandsData = SerializationUTF8.
                DeserializeCompressed<(Dictionary<int, WorldData> WorldData, CommandsData Commands)>(_textAsset.bytes).Commands;

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
