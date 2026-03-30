#if UNITY_EDITOR
using DVG.Sheets;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Tools.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.Editor
{
    public sealed class GlobalConfigLoader
    {
        private static readonly string PathFormat = "{0}/Scripts/SkyPirates/Shared/Resources/Configs/{1}.json";
        private static readonly string TableId = "1Ovc14Sui7WKz4-JLyYwa629HNSdWGwXNS2y4FphMKEA";
        private static readonly Sheet[] Sheets = new Sheet[]
        {
            new("UnitsStats", 3, 0),
            new("CactusesStats", 3, 292970565),
            new("TreesStats", 2, 1360315137),
            new("RocksStats", 2, 1427219889),
            new("GoodsStats", 1, 1938501206),
            new("FramedComponentDependencies", 1, 1488722279),
            new("ComponentDependencies", 1, 608707911),
            new("ComponentDefaults", 3, 519411523),
            new("SquadStats", 1, 1555186190),
            new("CameraConfig", 1, 715522005),
            new("UnitsInfos", 1, 1805816128),
        };

        [MenuItem("Tools/DVG/Configs/Sync", false, 0)]
        public static async Task Sync()
        {
            SheetLoader loader = new(TableId, Sheets);
            var result = await loader.LoadAsCsv();
            foreach (var item in result)
                Save(item.Key, item.Value.ToJsonString(SerializationUTF8.Options));

            var config = Parse(result);
            config = SerializeCheck(config);
            SaveGlobal(config);
            AssetDatabase.Refresh();
            Trace.TraceInformation("[GlobalConfigLoader] Sync completed");
        }


        private static string Parse(Dictionary<string, JsonArray> result)
        {
            try
            {
                JsonObject config = new();
                foreach (var item in result)
                    ParseConfigElement(config, item);

                return config.ToJsonString(SerializationUTF8.Options);
            }
            catch (Exception e)
            {
                Trace.TraceError($"[GlobalConfigLoader] Parse failed: {e.Message}\n{e.StackTrace}");
                throw new();
            }
        }

        private static void ParseConfigElement(JsonObject config, KeyValuePair<string, JsonArray> result)
        {
            var fieldType = typeof(GlobalConfig).GetField(result.Key).FieldType;
            if (typeof(IList).IsAssignableFrom(fieldType))
            {
                config[result.Key] = result.Value.ToList();
                return;
            }
            if (typeof(IDictionary).IsAssignableFrom(fieldType))
            {
                var keyTypeName = fieldType.BaseType.GetGenericArguments()[0].Name;
                config[result.Key] = result.Value.ToDictionary(keyTypeName);
                return;
            }
            else
            {
                config[result.Key] = result.Value.ToSingle();
            }
        }



        private static string SerializeCheck(string json)
        {
            try
            {
                var configObject = SerializationUTF8.Deserialize<GlobalConfig>(json);
                return SerializationUTF8.Serialize(configObject);
            }
            catch (Exception e)
            {
                Trace.TraceError($"[GlobalConfigLoader] Serialize check failed: {e.Message}\n{e.StackTrace}");
                throw new();
            }
        }

        private static void SaveGlobal(string json)
        {
            var path = string.Format(PathFormat, Application.dataPath, "GlobalConfig");
            File.WriteAllText(path, json);
        }

        private static void Save(string name, string json)
        {
            var path = string.Format(PathFormat, Application.dataPath, name);
            File.WriteAllText(path, json);
        }
    }
}
#endif