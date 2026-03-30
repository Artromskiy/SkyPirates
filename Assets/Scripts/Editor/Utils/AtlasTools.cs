using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DVG.Editor.Tools
{
    public class AtlasTools : EditorWindow
    {
        [MenuItem("Assets/DVG/Atlas Tools")]
        public static void Open() => GetWindow<AtlasTools>("Atlas Tools");

        private readonly List<Texture2D> _textures = new();
        private string _outputPath = "Assets/Atlas.png";
        private int _padding = 2;
        private bool _blackToAlpha = true;
        private AnimationCurve _alphaCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private int _cols = 4;
        private int _rows = 4;
        private bool _autoRows = true;
        private int _tileSizePOT = 64;

        private static readonly int[] _potSizes = { 8, 16, 32, 64, 128, 256, 512, 1024 };
        private Vector2 _scroll;

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Input Textures", EditorStyles.boldLabel);
            var dropRect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Drag textures here");
            HandleDrop(dropRect);

            for (int i = 0; i < _textures.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _textures[i] = (Texture2D)EditorGUILayout.ObjectField(
                    _textures[i], typeof(Texture2D), false,
                    GUILayout.Height(40), GUILayout.Width(40));
                if (_textures[i] != null)
                    EditorGUILayout.LabelField(_textures[i].name);
                if (GUILayout.Button("✕", GUILayout.Width(24))) { _textures.RemoveAt(i); break; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Texture")) _textures.Add(null);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

            _cols = EditorGUILayout.IntField("Columns (N)", _cols);
            _autoRows = EditorGUILayout.Toggle("Auto Rows", _autoRows);
            if (!_autoRows)
                _rows = EditorGUILayout.IntField("Rows (M)", _rows);
            else
                EditorGUILayout.LabelField("Rows", _textures.Count > 0
                    ? Mathf.CeilToInt((float)_textures.Count / Mathf.Max(_cols, 1)).ToString()
                    : "auto");

            int tileSizeIndex = Array.IndexOf(_potSizes, _tileSizePOT);
            if (tileSizeIndex < 0) tileSizeIndex = 3;
            var potLabels = Array.ConvertAll(_potSizes, x => x.ToString());
            tileSizeIndex = EditorGUILayout.Popup("Tile Size (POT)", tileSizeIndex, potLabels);
            _tileSizePOT = _potSizes[tileSizeIndex];

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Processing", EditorStyles.boldLabel);
            _padding = EditorGUILayout.IntField("Padding (px)", _padding);
            _blackToAlpha = EditorGUILayout.Toggle("HSV Value → Alpha", _blackToAlpha);
            if (_blackToAlpha)
                _alphaCurve = EditorGUILayout.CurveField("Alpha Curve", _alphaCurve);
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);

            int cols = Mathf.Max(_cols, 1);
            int rows = _autoRows ? Mathf.CeilToInt((float)Mathf.Max(_textures.Count, 1) / cols) : Mathf.Max(_rows, 1);
            int tileStep = _tileSizePOT + _padding * 2;
            int atlasW = NextPOT(cols * tileStep);
            int atlasH = NextPOT(rows * tileStep);
            EditorGUILayout.HelpBox($"Atlas size: {atlasW} x {atlasH} | Tiles: {cols} x {rows} @ {_tileSizePOT}px", MessageType.Info);

            EditorGUILayout.Space();
            GUI.enabled = _textures.Count > 0;
            if (GUILayout.Button("Build Atlas", GUILayout.Height(36))) Build();
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
        }

        private void HandleDrop(Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                    if (obj is Texture2D t) _textures.Add(t);
                e.Use();
            }
        }

        private void Build()
        {
            var readables = new List<Texture2D>();
            foreach (var src in _textures)
                if (src != null) readables.Add(GetReadable(src));

            if (readables.Count == 0) { Debug.LogWarning("[AtlasTools] No valid textures."); return; }

            int cols = Mathf.Max(_cols, 1);
            int rows = _autoRows ? Mathf.CeilToInt((float)readables.Count / cols) : Mathf.Max(_rows, 1);
            int tileSize = _tileSizePOT;
            int tileStep = tileSize + _padding * 2;
            int atlasW = NextPOT(cols * tileStep);
            int atlasH = NextPOT(rows * tileStep);

            int exactW = cols * tileStep;
            int exactH = rows * tileStep;

            var atlas = new Texture2D(exactW, exactH, TextureFormat.RGBA32, false);
            ClearTexture(atlas);

            for (int i = 0; i < readables.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;

                int px = col * tileStep + _padding;
                int py = (rows - 1 - row) * tileStep + _padding; // flip Y

                var bounds = GetContentBounds(readables[i]);
                var crop = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false);
                crop.SetPixels(readables[i].GetPixels(bounds.x, bounds.y, bounds.width, bounds.height));
                crop.Apply();

                var tile = ResizeBilinear(crop, tileSize, tileSize);

                if (_blackToAlpha)
                    ApplyValueToAlpha(tile);

                atlas.SetPixels(px, py, tileSize, tileSize, tile.GetPixels());
            }

            atlas.Apply();

            var finalAtlas = ResizeBilinear(atlas, atlasW, atlasH);

            var dir = Path.GetDirectoryName(_outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllBytes(_outputPath, finalAtlas.EncodeToPNG());
            AssetDatabase.Refresh();

            Debug.Log($"[AtlasTools] Done → {_outputPath} ({atlasW}x{atlasH}, {cols}x{rows} tiles @ {tileSize}px)");
        }

        private static Texture2D GetReadable(Texture2D src)
        {
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var r = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            r.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            r.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return r;
        }

        private static RectInt GetContentBounds(Texture2D tex)
        {
            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            int minX = w, minY = h, maxX = 0, maxY = 0;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = px[y * w + x];
                    if (c.r > 10 || c.g > 10 || c.b > 10 || c.a > 10)
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }

            if (minX > maxX) return new RectInt(0, 0, 1, 1);

            int bw = Mathf.Min(NextPOT(maxX - minX + 1), w);
            int bh = Mathf.Min(NextPOT(maxY - minY + 1), h);
            return new RectInt(minX, minY, bw, bh);
        }

        private static Texture2D ResizeBilinear(Texture2D src, int newW, int newH)
        {
            var dst = new Texture2D(newW, newH, TextureFormat.RGBA32, false);
            var px = new Color[newW * newH];
            float sx = (float)src.width / newW;
            float sy = (float)src.height / newH;

            for (int y = 0; y < newH; y++)
                for (int x = 0; x < newW; x++)
                    px[y * newW + x] = src.GetPixelBilinear((x + 0.5f) * sx / src.width,
                                                             (y + 0.5f) * sy / src.height);
            dst.SetPixels(px);
            dst.Apply();
            return dst;
        }

        private void ApplyValueToAlpha(Texture2D tex)
        {
            var px = tex.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                float value = Mathf.Max(c.r, c.g, c.b);
                float inv = value > 0.0001f ? 1f / value : 0f;
                float alpha = _alphaCurve != null ? _alphaCurve.Evaluate(value) : value;
                px[i] = new Color(c.r * inv, c.g * inv, c.b * inv, alpha);
            }
            tex.SetPixels(px);
            tex.Apply();
        }

        private static void ClearTexture(Texture2D tex)
        {
            tex.SetPixels(new Color[tex.width * tex.height]);
            tex.Apply();
        }

        private static int NextPOT(int v)
        {
            if (v <= 0) return 1;
            v--;
            v |= v >> 1; v |= v >> 2; v |= v >> 4; v |= v >> 8; v |= v >> 16;
            return v + 1;
        }
    }
}