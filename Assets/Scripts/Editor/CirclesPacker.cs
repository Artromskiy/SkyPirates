using DVG.Json.Editor;
using DVG.Maths;
using DVG.SkyPirates.Shared.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DVG.Editor
{
    [CreateAssetMenu(fileName = "CirclesPacker", menuName = "Tools/CirclesPacker")]
    public class CirclesPacker : ScriptableObject
    {
        [SerializeField]
        private TextAsset[] _packedCircles;
        [SerializeField]
        private string _folderPath;

        private static readonly string[] SplitLines = new string[] { "\r\n", "\r", "\n" };
        private static readonly string[] WhiteSpace = new string[] { " " };

        private string FolderPath => Path.Combine(Application.dataPath, _folderPath);

        [ContextMenu("Pack Circles to Models")]
        public void PackCirclesToModels()
        {
            foreach (var item in _packedCircles)
            {
                var pos = new List<vec2>();
                var lines = item.text.Split(SplitLines, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.IsNullOrEmpty(lines[i]) || string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    var split = lines[i].Split(WhiteSpace, StringSplitOptions.RemoveEmptyEntries);

                    if (split.Length != 3)
                        continue;

                    var culture = CultureInfo.InvariantCulture;
                    pos.Add(new vec2(float.Parse(split[1], culture), float.Parse(split[2], culture)));
                }
                float minSqrLength = float.MaxValue;
                for (int i = 0; i < pos.Count; i++)
                {
                    for (int j = i + 1; j < pos.Count; j++)
                    {
                        var sqrLength = vec2.sqrlength(pos[i], pos[j]);
                        if (sqrLength < minSqrLength)
                            minSqrLength = sqrLength;
                    }
                }
                float radius = pos.Count == 1 ? 1 : math.sqrt(minSqrLength) / 2f;
                var modifier = 1f / radius;
                pos = pos.ConvertAll(v => (v * modifier));
                var model = new PackedCirclesModel(modifier, pos.ToArray());
                var json = JsonUtility.ToJson(model);
                var filePath = Path.Combine(FolderPath, "PackedCirclesModel" + pos.Count);
                var filePathWithExtension = Path.ChangeExtension(filePath, ".json");
                File.WriteAllText(filePathWithExtension, json);
            }
        }

        [ContextMenu("Update Json Types")]
        public void UpdateJsonTypes()
        {
            foreach (var item in Directory.EnumerateFiles(FolderPath))
            {
                if (item.EndsWith(".meta"))
                    continue;
                var relativePath = Path.GetRelativePath(Application.dataPath, item);
                relativePath = Path.Combine("Assets", relativePath);
                var guid = AssetDatabase.AssetPathToGUID(relativePath);
                if (string.IsNullOrEmpty(guid))
                    continue;
                JsonTypeCache.Instance.SetType(guid, typeof(PackedCirclesModel).FullName);
            }

        }
    }
}
