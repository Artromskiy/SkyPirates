#if UNITY_EDITOR
using DVG.Sheets;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Tools.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using UnityEditor;

namespace DVG.SkyPirates.Tooling.Editor
{
    internal static class GoogleSheetsConfigSync
    {
        public static async Task<int> Sync(GoogleSheetsConfigLoader loader, Action<int, int> progress)
        {
            try
            {
                return await SyncCore(loader, progress);
            }
            catch (Exception exception)
            {
                Delta.Diagnostics.Debug.Error(exception, context: loader);
                throw;
            }
        }

        private static async Task<int> SyncCore(GoogleSheetsConfigLoader loader, Action<int, int> progress)
        {
            if (loader.Sheets == null || loader.Sheets.Count == 0)
                throw new InvalidOperationException("The loader has no sheet sources.");
            if (string.IsNullOrWhiteSpace(loader.OutputDirectory))
                throw new InvalidOperationException("Set the output directory on the loader asset.");

            var requests = new List<(string Name, SheetLoader Loader, Sheet Sheet, char Separator)>();
            var tableLoaders = new Dictionary<string, SheetLoader>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in loader.Sheets)
            {
                if (string.IsNullOrWhiteSpace(source.Name))
                    throw new InvalidOperationException("Every sheet must have a name.");
                if (!names.Add(source.Name))
                    throw new InvalidOperationException($"Sheet name '{source.Name}' is used more than once.");

                (string tableId, int sheetId) = ParseSheetUrl(source.Url);
                char separator = source.Separator == DsvSeparator.Tab ? '\t' : ',';
                if (!tableLoaders.TryGetValue(tableId, out var tableLoader))
                    tableLoaders.Add(tableId, tableLoader = new SheetLoader(tableId));

                var sheet = new Sheet(source.Name, source.HeaderRows, sheetId);
                requests.Add((source.Name, tableLoader, sheet, separator));
            }

            var loadedCount = 0;
            var loadTasks = requests.Select(async request =>
            {
                var data = await request.Loader.LoadAsDsv(request.Sheet, request.Separator);
                progress(++loadedCount, requests.Count);
                return (Name: request.Name, Data: data);
            }).ToList();
            var loadedSheets = await Task.WhenAll(loadTasks);
            var result = new Dictionary<string, JsonArray>();
            foreach (var loaded in loadedSheets)
                result.Add(loaded.Name, loaded.Data);

            var config = Parse(result);
            var configObject = SerializationUTF8.Deserialize<GlobalConfig>(config)
                ?? throw new InvalidOperationException("Deserializing the combined config returned null.");
            var outputs = new Dictionary<string, string>();
            foreach (var item in result)
                outputs.Add(item.Key + ".json", item.Value.ToJsonString(SerializationUTF8.Options));
            outputs.Add("GlobalConfig.json", SerializationUTF8.Serialize(configObject));

            var outputDirectory = Path.GetFullPath(loader.OutputDirectory);
            Directory.CreateDirectory(outputDirectory);
            foreach (var output in outputs)
                File.WriteAllText(Path.Combine(outputDirectory, output.Key), output.Value);

            AssetDatabase.Refresh();
            return requests.Count;
        }

        private static string Parse(Dictionary<string, JsonArray> result)
        {
            var config = new JsonObject();
            foreach (var item in result)
            {
                var field = typeof(GlobalConfig).GetField(item.Key)
                    ?? throw new InvalidOperationException($"GlobalConfig has no field '{item.Key}'.");
                var fieldType = field.FieldType;
                var genericType = fieldType.IsGenericType ? fieldType.GetGenericTypeDefinition() : null;
                if (typeof(IList).IsAssignableFrom(fieldType)
                    || genericType == typeof(IReadOnlyList<>)
                    || genericType == typeof(IReadOnlyCollection<>))
                {
                    config[item.Key] = item.Value.ToList();
                }
                else if (typeof(IDictionary).IsAssignableFrom(fieldType)
                    || genericType == typeof(IReadOnlyDictionary<,>))
                {
                    var keyType = genericType == typeof(IReadOnlyDictionary<,>)
                        ? fieldType.GetGenericArguments()[0]
                        : fieldType.BaseType!.GetGenericArguments()[0];
                    config[item.Key] = item.Value.ToDictionary(keyType.Name);
                }
                else
                {
                    config[item.Key] = item.Value.ToSingle();
                }
            }

            return config.ToJsonString(SerializationUTF8.Options);
        }

        private static (string tableId, int sheetId) ParseSheetUrl(string input)
        {
            if (!Uri.TryCreate(input, UriKind.Absolute, out var uri)
                || !string.Equals(uri.Host, "docs.google.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Enter a direct URL from docs.google.com.");
            }

            string[] path = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int spreadsheetsIndex = Array.IndexOf(path, "spreadsheets");
            int documentIndex = spreadsheetsIndex < 0 ? -1 : Array.IndexOf(path, "d", spreadsheetsIndex + 1);
            if (documentIndex < 0 || documentIndex + 1 >= path.Length || path[documentIndex + 1] == "e")
                throw new InvalidOperationException("The URL must contain a standard Google Sheets document ID.");

            string? gid = GetQueryValue(uri.Query, "gid") ?? GetQueryValue(uri.Fragment, "gid");
            if (!int.TryParse(gid, out int sheetId))
                throw new InvalidOperationException("The tab ID (gid) is missing or invalid in the URL.");

            return (Uri.UnescapeDataString(path[documentIndex + 1]), sheetId);
        }

        private static string? GetQueryValue(string query, string key)
        {
            foreach (string pair in query.TrimStart('?', '#').Split('&'))
            {
                int separatorIndex = pair.IndexOf('=');
                if (separatorIndex >= 0
                    && string.Equals(pair.Substring(0, separatorIndex), key, StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(pair.Substring(separatorIndex + 1));
                }
            }

            return null;
        }
    }
}
#endif
