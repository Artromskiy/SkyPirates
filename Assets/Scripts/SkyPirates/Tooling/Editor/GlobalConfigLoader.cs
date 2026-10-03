#if UNITY_EDITOR
using DVG.Sheets;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Tools.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DVG.SkyPirates.Tooling.Editor
{
    public sealed class GlobalConfigLoader : EditorWindow
    {
        private const string LayoutPath = "Assets/Scripts/SkyPirates/Tooling/Editor/GoogleSheetsConfigWindow.uxml";
        private const string ConfigDirectory = "Scripts/SkyPirates/Shared/Resources/Configs";

        private ObjectField _loaderField = null!;
        private Button _loadButton = null!;
        private Label _statusLabel = null!;

        [MenuItem("Tools/DVG/Configs/Sync", false, 0)]
        public static void Open()
        {
            var window = GetWindow<GlobalConfigLoader>();
            window.titleContent = new GUIContent("Config Loader");
            window.minSize = new Vector2(420, 180);
        }

        public void CreateGUI()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            if (layout == null)
            {
                rootVisualElement.Add(new HelpBox($"UI layout not found: {LayoutPath}", HelpBoxMessageType.Error));
                return;
            }

            layout.CloneTree(rootVisualElement);
            _loaderField = rootVisualElement.Q<ObjectField>("loader-field");
            _loadButton = rootVisualElement.Q<Button>("sync-button");
            _statusLabel = rootVisualElement.Q<Label>("status-label");
            _loaderField.objectType = typeof(GoogleSheetsConfigLoader);
            _loaderField.allowSceneObjects = false;
            _loadButton.clicked += Load;

            var loaders = AssetDatabase.FindAssets("t:GoogleSheetsConfigLoader");
            if (loaders.Length == 1)
                _loaderField.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<GoogleSheetsConfigLoader>(AssetDatabase.GUIDToAssetPath(loaders[0])));
        }

        private async void Load()
        {
            if (_loaderField.value is not GoogleSheetsConfigLoader loader)
            {
                _statusLabel.text = "Select a Google Sheets loader asset.";
                return;
            }

            _loadButton.SetEnabled(false);
            try
            {
                _statusLabel.text = "Loading sheets…";
                var result = new Dictionary<string, JsonArray>();
                var tableLoaders = new Dictionary<string, SheetLoader>();
                foreach (var source in loader.Sheets)
                {
                    var (tableId, sheetId) = ParseSheetUrl(source.Url);
                    var separator = source.Separator == DsvSeparator.Tab ? '\t' : ',';
                    if (!tableLoaders.TryGetValue(tableId, out var tableLoader))
                        tableLoaders.Add(tableId, tableLoader = new SheetLoader(tableId));

                    var sheet = new Sheet(source.Name, source.HeaderRows, sheetId);
                    result.Add(source.Name, await tableLoader.LoadAsDsv(sheet, separator));
                }

                var config = SerializeCheck(Parse(result));
                foreach (var item in result)
                    Save(item.Key, item.Value.ToJsonString(SerializationUTF8.Options));

                Save("GlobalConfig", config);
                AssetDatabase.Refresh();
                _statusLabel.text = "Configs loaded.";
            }
            catch (Exception exception)
            {
                _statusLabel.text = exception.Message;
            }
            finally
            {
                _loadButton.SetEnabled(true);
            }
        }

        private static string Parse(Dictionary<string, JsonArray> result)
        {
            var config = new JsonObject();
            foreach (var item in result)
                ParseConfigElement(config, item);
            return config.ToJsonString(SerializationUTF8.Options);
        }

        private static void ParseConfigElement(JsonObject config, KeyValuePair<string, JsonArray> result)
        {
            var field = typeof(GlobalConfig).GetField(result.Key)
                ?? throw new InvalidOperationException($"GlobalConfig has no field '{result.Key}'.");
            var fieldType = field.FieldType;
            var genericType = fieldType.IsGenericType ? fieldType.GetGenericTypeDefinition() : null;
            if (typeof(IList).IsAssignableFrom(fieldType)
                || genericType == typeof(IReadOnlyList<>)
                || genericType == typeof(IReadOnlyCollection<>))
                config[result.Key] = result.Value.ToList();
            else if (typeof(IDictionary).IsAssignableFrom(fieldType) || genericType == typeof(IReadOnlyDictionary<,>))
            {
                var keyType = genericType == typeof(IReadOnlyDictionary<,>)
                    ? fieldType.GetGenericArguments()[0]
                    : fieldType.BaseType!.GetGenericArguments()[0];
                config[result.Key] = result.Value.ToDictionary(keyType.Name);
            }
            else
                config[result.Key] = result.Value.ToSingle();
        }

        private static string SerializeCheck(string json)
        {
            var config = SerializationUTF8.Deserialize<GlobalConfig>(json);
            return SerializationUTF8.Serialize(config);
        }

        private static (string tableId, int sheetId) ParseSheetUrl(string input)
        {
            if (!Uri.TryCreate(input, UriKind.Absolute, out var uri)
                || !string.Equals(uri.Host, "docs.google.com", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Enter a direct URL from docs.google.com.");

            var path = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var spreadsheetsIndex = Array.IndexOf(path, "spreadsheets");
            var documentIndex = spreadsheetsIndex < 0 ? -1 : Array.IndexOf(path, "d", spreadsheetsIndex + 1);
            if (documentIndex < 0 || documentIndex + 1 >= path.Length || path[documentIndex + 1] == "e")
                throw new InvalidOperationException("The URL must contain a standard Google Sheets document ID.");

            var gid = GetQueryValue(uri.Query, "gid") ?? GetQueryValue(uri.Fragment, "gid");
            if (!int.TryParse(gid, out var sheetId))
                throw new InvalidOperationException("The tab ID (gid) is missing or invalid in the URL.");

            return (Uri.UnescapeDataString(path[documentIndex + 1]), sheetId);
        }

        private static string? GetQueryValue(string query, string key)
        {
            foreach (var pair in query.TrimStart('?', '#').Split('&'))
            {
                var separatorIndex = pair.IndexOf('=');
                if (separatorIndex >= 0 && string.Equals(pair.Substring(0, separatorIndex), key, StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(pair.Substring(separatorIndex + 1));
            }

            return null;
        }

        private static void Save(string name, string json)
        {
            var path = Path.Combine(Application.dataPath, ConfigDirectory, name + ".json");
            File.WriteAllText(path, json);
        }
    }
}
#endif
