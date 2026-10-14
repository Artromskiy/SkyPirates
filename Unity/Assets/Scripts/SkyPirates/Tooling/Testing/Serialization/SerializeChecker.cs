using SkyPirates.Shared.Commands;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.Tools.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SkyPirates.Tooling.Testing
{
    public class SerializeChecker : MonoBehaviour
    {
        [SerializeField]
        private TextAsset _textAsset;

        [ContextMenu("Serialization/Test Serialize")]
        private void TestSerialize()
        {
            if (_textAsset == null)
            {
                Debug.LogError("Assign a compressed world and command data asset before testing serialization.", this);
                return;
            }

            var commandsData = SerializationUTF8.
                DeserializeCompressed<(Dictionary<int, WorldData> WorldData, CommandsData Commands)>(_textAsset.bytes).Commands;

            string commandsDataJson = SerializationUTF8.SerializeOrdered(commandsData);
            File.WriteAllText(ToolingOutput.GetJsonPath("TimelineDebug", "CommandsData"), commandsDataJson);
        }
    }
}
