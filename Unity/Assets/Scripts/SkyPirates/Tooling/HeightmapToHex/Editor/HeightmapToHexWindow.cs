using System;
using System.Collections.Generic;
using SlyPirates.Client.Views.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlyPirates.Tooling.HeightmapToHex.Editor
{
    [Serializable]
    internal sealed class HeightColorRule
    {
        public string name;
        [Range(0f, 1f)] public float minimumHeight;
        public Color color;
    }

    public sealed class HeightmapToHexWindow : EditorWindow
    {
        private const string RootName = "Generated Island Hex Map";
        private const string TileViewPath = "Prefabs/HexTile/HexTileView";
        private const float LandThreshold = 0.13f;

        [SerializeField] private Texture2D _heightmap;
        [SerializeField] private int _columns = 48, _rows = 48, _maximumElevation = 8, _heightSteps = 10;
        [SerializeField] private List<HeightColorRule> _palette = new List<HeightColorRule>();
        private ReorderableList _rules;
        private Vector2 _scroll;
        private string _status;

        [MenuItem("Sky Pirates/Tools/Heightmap To Hex")]
        private static void Open() => GetWindow<HeightmapToHexWindow>("Heightmap To Hex");

        private void OnEnable()
        {
            _palette ??= new List<HeightColorRule>();
            if (_palette.Count == 0)
            {
                _palette.AddRange(DefaultPalette());
            }

            _heightmap ??= AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GeneratedHexIslands/HeightMaps/IslandHeightmap_RoughMountain.png");
            _rules = new ReorderableList(_palette, typeof(HeightColorRule), true, true, true, true);
            _rules.drawHeaderCallback = r => EditorGUI.LabelField(r, "Height palette (drag rows to reorder)");
            _rules.elementHeight = EditorGUIUtility.singleLineHeight + 6;
            _rules.drawElementCallback = DrawRule;
            _rules.onAddCallback = list => _palette.Add(new HeightColorRule { name = "Color " + (_palette.Count + 1), minimumHeight = _palette[_palette.Count - 1].minimumHeight, color = Color.white });
            _rules.onRemoveCallback = list => { if (_palette.Count > 1)
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                }
            };
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _heightmap = (Texture2D)EditorGUILayout.ObjectField("Heightmap", _heightmap, typeof(Texture2D), false);
            _columns = EditorGUILayout.IntField("Columns", _columns);
            _rows = EditorGUILayout.IntField("Rows", _rows);
            _maximumElevation = EditorGUILayout.IntField("Maximum elevation", _maximumElevation);
            _heightSteps = EditorGUILayout.IntField("Height steps", _heightSteps);
            _rules.DoLayoutList();
            if (GUILayout.Button("Generate In Open Scene", GUILayout.Height(32)))
            {
                Generate();
            }

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.HelpBox(_status, MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRule(Rect rect, int index, bool active, bool focused)
        {
            HeightColorRule rule = _palette[index];
            rect.y += 2;
            float width = (rect.width - 8) / 3;
            rule.name = EditorGUI.TextField(new Rect(rect.x, rect.y, width, rect.height), rule.name);
            rule.minimumHeight = EditorGUI.Slider(new Rect(rect.x + width + 4, rect.y, width, rect.height), rule.minimumHeight, 0f, 1f);
            rule.color = EditorGUI.ColorField(new Rect(rect.x + 2 * (width + 4), rect.y, width, rect.height), GUIContent.none, rule.color, false, false, false);
        }

        private void Generate()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject prefab = Resources.Load<GameObject>(TileViewPath);
            if (_heightmap == null || prefab == null || !scene.IsValid() || !scene.isLoaded)
            {
                _status = _heightmap == null ? "Select a heightmap." : "HexTileView prefab or active scene is unavailable.";
                return;
            }

            Texture2D map = ReadTexture(_heightmap);
            GameObject root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Generate Island Hex Map");
            foreach (GameObject old in scene.GetRootGameObjects())
            {
                if (old != root && old.name.StartsWith(RootName, StringComparison.Ordinal))
                {
                    DestroyImmediate(old);
                }
            }

            int count = 0;
            for (int x = 0; x < _columns; x++)
            for (int y = 0; y < _rows; y++)
            {
                int column = x - (_columns - 1) / 2, parity = column & 1;
                float halfWidth = (_columns - 1) * 0.75f + 1f, halfDepth = _rows * Mathf.Sqrt(3f) / 2f;
                float u = (column * 1.5f + halfWidth) / (2f * halfWidth);
                float v = ((y - (_rows - 1) * 0.5f + parity * 0.5f) * Mathf.Sqrt(3f) + halfDepth) / (2f * halfDepth);
                float height = map.GetPixelBilinear(u, v).grayscale;
                if (height <= LandThreshold)
                {
                    continue;
                }

                float land = Mathf.InverseLerp(LandThreshold, 1f, height);
                int zone = 0;
                for (int i = 1; i < _palette.Count; i++)
                {
                    if (land >= _palette[i].minimumHeight)
                    {
                        zone = i;
                    }
                }

                float steps = Mathf.Abs((float)_heightSteps);
                float steppedHeight = steps == 0f ? land : Mathf.Round(land * steps) / steps;
                int elevation = Mathf.Max(1, Mathf.RoundToInt(steppedHeight * _maximumElevation));
                for (int level = 0; level <= elevation; level++)
                {
                    GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    tile.transform.SetParent(root.transform, true);
                    tile.GetComponent<HexTileComponentView>().SetEditorPreview(column, y - (_rows - 1) / 2, level, "Land", _palette[zone].color);
                    count++;
                }
            }

            DestroyImmediate(map);
            EditorSceneManager.MarkSceneDirty(scene);
            _status = $"Generated {count:N0} project HexTile prefabs in '{scene.name}'. Save the scene to keep them.";
        }

        private static Texture2D ReadTexture(Texture2D source)
        {
            RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return copy;
        }

        private static IEnumerable<HeightColorRule> DefaultPalette()
        {
            yield return new HeightColorRule { name = "Shore", color = new Color(.78f, .66f, .42f) };
            yield return new HeightColorRule { name = "Lowland", minimumHeight = .2f, color = new Color(.38f, .62f, .28f) };
            yield return new HeightColorRule { name = "Highland", minimumHeight = .45f, color = new Color(.27f, .48f, .25f) };
            yield return new HeightColorRule { name = "Mountain", minimumHeight = .7f, color = new Color(.48f, .46f, .41f) };
            yield return new HeightColorRule { name = "Peak", minimumHeight = .9f, color = new Color(.9f, .9f, .86f) };
        }
    }
}
