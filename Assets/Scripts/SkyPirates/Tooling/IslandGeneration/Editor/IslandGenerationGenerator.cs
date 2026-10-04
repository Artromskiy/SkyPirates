#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DVG.SkyPirates.Tooling.IslandGeneration.Editor
{
    public static class IslandGenerationGenerator
    {
        private const int MaximumHexCount = 262144;
        private const int ThreadGroupSize = 8;
        private const float PreviewHexRadius = 8f;
        private const float PreviewPadding = 8f;
        private const float SqrtThree = 1.7320508f;
        private const float InverseSqrtThree = 0.57735027f;
        private const float HalfSqrtThree = 0.8660254f;

        private static readonly int[] NeighborQ = { 1, 0, -1, -1, 0, 1 };
        private static readonly int[] NeighborR = { 0, 1, 1, 0, -1, -1 };

        [StructLayout(LayoutKind.Sequential)]
        private struct RawMapSample
        {
            public float LandMask;
            public float Height;
        }

        private struct GenerationSettings
        {
            public int Columns;
            public int Rows;
            public int Seed;
            public float IslandScale;
            public float OutlinePower;
            public float BoundaryWarp;
            public float BoundaryFrequency;
            public float CenterOffsetX;
            public float CenterOffsetY;
            public float MaximumHeight;
            public int HeightLevels;
            public float CoastExponent;
            public float HeightNoiseStrength;
            public float HeightNoiseFrequency;
            public int ThermalErosionIterations;
            public float TalusHeight;
            public float ErosionRate;

            public static GenerationSettings From(IslandGenerationProfile profile)
            {
                return new GenerationSettings
                {
                    Columns = profile.Columns,
                    Rows = profile.Rows,
                    Seed = profile.Seed,
                    IslandScale = profile.IslandScale,
                    OutlinePower = profile.OutlinePower,
                    BoundaryWarp = profile.BoundaryWarp,
                    BoundaryFrequency = profile.BoundaryFrequency,
                    CenterOffsetX = profile.CenterOffsetX,
                    CenterOffsetY = profile.CenterOffsetY,
                    MaximumHeight = profile.MaximumHeight,
                    HeightLevels = profile.HeightLevels,
                    CoastExponent = profile.CoastExponent,
                    HeightNoiseStrength = profile.HeightNoiseStrength,
                    HeightNoiseFrequency = profile.HeightNoiseFrequency,
                    ThermalErosionIterations = profile.ThermalErosionIterations,
                    TalusHeight = profile.TalusHeight,
                    ErosionRate = profile.ErosionRate
                };
            }
        }

        public sealed class GenerationOperation
        {
            private readonly IslandGenerationProfile _profile;
            private readonly GenerationSettings _settings;
            private readonly Action<HexIslandMap> _completed;
            private readonly Action<Exception> _failed;
            private readonly Action<GenerationOperation> _finishedCallback;
            private ComputeBuffer _buffer;
            private bool _cancelled;
            private bool _finished;

            internal GenerationOperation(
                IslandGenerationProfile profile,
                Action<HexIslandMap> completed,
                Action<Exception> failed,
                Action<GenerationOperation> finishedCallback)
            {
                _profile = profile;
                _settings = GenerationSettings.From(profile);
                _completed = completed;
                _failed = failed;
                _finishedCallback = finishedCallback;
            }

            public bool IsFinished => _finished;

            public void Cancel()
            {
                _cancelled = true;
            }

            public void Start(ComputeShader shader)
            {
                if (_finished || _buffer != null)
                    throw new InvalidOperationException("This island generation operation has already started.");

                try
                {
                    var kernel = shader.FindKernel("GenerateField");
                    var count = checked(_settings.Columns * _settings.Rows);
                    _buffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(RawMapSample)), ComputeBufferType.Structured);
                    BindSettings(shader, kernel, _buffer, _settings);
                    shader.Dispatch(
                        kernel,
                        Mathf.CeilToInt(_settings.Columns / (float)ThreadGroupSize),
                        Mathf.CeilToInt(_settings.Rows / (float)ThreadGroupSize),
                        1);

                    AsyncGPUReadback.Request(_buffer, OnReadbackCompleted);
                }
                catch (Exception exception)
                {
                    ReleaseBuffer();
                    _finished = true;
                    throw new InvalidOperationException("Could not dispatch the island shape compute shader.", exception);
                }
            }

            private void OnReadbackCompleted(AsyncGPUReadbackRequest request)
            {
                try
                {
                    if (request.hasError)
                        throw new InvalidOperationException("GPU readback failed. Check compute shader support and graphics driver logs.");

                    ReleaseBuffer();
                    if (_cancelled)
                    {
                        _finished = true;
                        _finishedCallback?.Invoke(this);
                        return;
                    }

                    var nativeSamples = request.GetData<RawMapSample>();
                    var fieldSamples = new RawMapSample[nativeSamples.Length];
                    nativeSamples.CopyTo(fieldSamples);
                    var cells = QuantizeFieldToHexes(_settings, fieldSamples);
                    var map = SaveMap(_profile, _settings, cells);
                    _finished = true;
                    _completed?.Invoke(map);
                    _finishedCallback?.Invoke(this);
                }
                catch (Exception exception)
                {
                    FinishWithError(exception);
                }
            }

            private void FinishWithError(Exception exception)
            {
                ReleaseBuffer();
                _finished = true;
                if (!_cancelled)
                    _failed?.Invoke(exception);
                _finishedCallback?.Invoke(this);
            }

            private void ReleaseBuffer()
            {
                if (_buffer == null)
                    return;

                _buffer.Release();
                _buffer = null;
            }
        }

        public static GenerationOperation CreateOperation(
            IslandGenerationProfile profile,
            Action<HexIslandMap> completed,
            Action<Exception> failed,
            Action<GenerationOperation> finished)
        {
            Validate(profile, true);
            return new GenerationOperation(profile, completed, failed, finished);
        }

        private static HexIslandCell[] QuantizeFieldToHexes(GenerationSettings settings, RawMapSample[] fieldSamples)
        {
            var count = checked(settings.Columns * settings.Rows);
            if (fieldSamples.Length != count)
                throw new InvalidOperationException("The generated map field has an unexpected sample count.");

            var land = new bool[count];
            var qCoordinates = new int[count];
            var rCoordinates = new int[count];
            var heights = new float[count];
            var rawHeights = new float[count];
            var maxLevel = settings.HeightLevels - 1;
            var heightUnit = settings.MaximumHeight / maxLevel;
            var centerX = (settings.Columns - 1) * 0.5f + 0.25f;
            var halfWidth = Mathf.Max((settings.Columns - 1) * 0.5f + 0.25f, 0.5f);
            var centerR = (settings.Rows - 1) * 0.5f - Mathf.Floor(settings.Rows * 0.5f);
            var halfHeight = Mathf.Max((settings.Rows - 1) * 0.5f, 0.5f);

            var indicesByAxialCoordinate = new Dictionary<long, int>(count);
            for (var row = 0; row < settings.Rows; row++)
            {
                var r = row - settings.Rows / 2;
                var qOffset = (r - (r & 1)) / 2;
                for (var column = 0; column < settings.Columns; column++)
                {
                    var index = row * settings.Columns + column;
                    var q = column - qOffset;
                    qCoordinates[index] = q;
                    rCoordinates[index] = r;
                    indicesByAxialCoordinate[CoordinateKey(q, r)] = index;

                    var worldX = q + r * 0.5f;
                    var normalizedX = (worldX - centerX) / halfWidth;
                    var normalizedY = (r - centerR) / halfHeight;
                    var fieldSample = SampleField(fieldSamples, settings.Columns, settings.Rows, normalizedX, normalizedY);
                    land[index] = fieldSample.LandMask >= 0.5f;
                    if (!land[index])
                        continue;

                    rawHeights[index] = Mathf.Clamp(fieldSample.Height, 0f, settings.MaximumHeight);
                    var initialLevel = Mathf.RoundToInt(rawHeights[index] / heightUnit);
                    heights[index] = Mathf.Clamp(initialLevel, 1, maxLevel);
                }
            }

            var neighborIndices = new int[count * 6];
            for (var index = 0; index < count; index++)
            {
                for (var direction = 0; direction < 6; direction++)
                {
                    var q = qCoordinates[index] + NeighborQ[direction];
                    var r = rCoordinates[index] + NeighborR[direction];
                    neighborIndices[index * 6 + direction] = indicesByAxialCoordinate.TryGetValue(CoordinateKey(q, r), out var neighbor)
                        ? neighbor
                        : -1;
                }
            }

            Erode(heights, land, neighborIndices, settings, maxLevel);
            var cells = new HexIslandCell[count];
            for (var i = 0; i < count; i++)
            {
                var level = land[i]
                    ? Mathf.Clamp(Mathf.RoundToInt(heights[i]), 1, maxLevel)
                    : 0;
                var finalHeight = level * heightUnit;
                cells[i] = new HexIslandCell(
                    qCoordinates[i],
                    rCoordinates[i],
                    land[i],
                    level,
                    rawHeights[i],
                    finalHeight);
            }

            return cells;
        }

        private static RawMapSample SampleField(
            RawMapSample[] fieldSamples,
            int width,
            int height,
            float normalizedX,
            float normalizedY)
        {
            var sampleX = Mathf.Clamp((normalizedX + 1f) * 0.5f * width - 0.5f, 0f, width - 1f);
            var sampleY = Mathf.Clamp((normalizedY + 1f) * 0.5f * height - 0.5f, 0f, height - 1f);
            var x0 = Mathf.FloorToInt(sampleX);
            var y0 = Mathf.FloorToInt(sampleY);
            var x1 = Mathf.Min(x0 + 1, width - 1);
            var y1 = Mathf.Min(y0 + 1, height - 1);
            var blendX = sampleX - x0;
            var blendY = sampleY - y0;

            var topLeft = fieldSamples[y0 * width + x0];
            var topRight = fieldSamples[y0 * width + x1];
            var bottomLeft = fieldSamples[y1 * width + x0];
            var bottomRight = fieldSamples[y1 * width + x1];

            return new RawMapSample
            {
                LandMask = Mathf.Lerp(
                    Mathf.Lerp(topLeft.LandMask, topRight.LandMask, blendX),
                    Mathf.Lerp(bottomLeft.LandMask, bottomRight.LandMask, blendX),
                    blendY),
                Height = Mathf.Lerp(
                    Mathf.Lerp(topLeft.Height, topRight.Height, blendX),
                    Mathf.Lerp(bottomLeft.Height, bottomRight.Height, blendX),
                    blendY)
            };
        }

        private static void Validate(IslandGenerationProfile profile, bool checkAsyncReadback)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (!SystemInfo.supportsComputeShaders)
                throw new InvalidOperationException("This editor graphics device does not support compute shaders.");
            if (checkAsyncReadback && !SystemInfo.supportsAsyncGPUReadback)
                throw new InvalidOperationException("This graphics device does not support asynchronous GPU readback, so generation cannot run without blocking the editor.");
            if (profile.Columns < 16 || profile.Rows < 16)
                throw new InvalidOperationException("The hex grid must be at least 16 × 16.");
            if ((long)profile.Columns * profile.Rows > MaximumHexCount)
                throw new InvalidOperationException($"The hex grid may contain at most {MaximumHexCount:N0} cells.");
            if (profile.MaximumHeight <= 0f)
                throw new InvalidOperationException("Maximum Height must be greater than zero.");
            if (profile.HeightLevels < 2)
                throw new InvalidOperationException("Height Levels must be at least 2.");
        }

        private static void BindSettings(
            ComputeShader shader,
            int kernel,
            ComputeBuffer buffer,
            GenerationSettings settings)
        {
            shader.SetInt("_FieldColumns", settings.Columns);
            shader.SetInt("_FieldRows", settings.Rows);
            shader.SetInt("_Seed", settings.Seed);
            shader.SetFloat("_IslandScale", settings.IslandScale);
            shader.SetFloat("_OutlinePower", settings.OutlinePower);
            shader.SetFloat("_BoundaryWarp", settings.BoundaryWarp);
            shader.SetFloat("_BoundaryFrequency", settings.BoundaryFrequency);
            shader.SetFloat("_CenterOffsetX", settings.CenterOffsetX);
            shader.SetFloat("_CenterOffsetY", settings.CenterOffsetY);
            shader.SetFloat("_MaximumHeight", settings.MaximumHeight);
            shader.SetFloat("_CoastExponent", settings.CoastExponent);
            shader.SetFloat("_HeightNoiseStrength", settings.HeightNoiseStrength);
            shader.SetFloat("_HeightNoiseFrequency", settings.HeightNoiseFrequency);
            shader.SetBuffer(kernel, "_MapField", buffer);
        }

        private static void Erode(
            float[] heights,
            bool[] land,
            int[] neighborIndices,
            GenerationSettings settings,
            int maxLevel)
        {
            if (settings.ThermalErosionIterations == 0 || settings.ErosionRate <= 0f)
                return;

            var talusInLevels = settings.TalusHeight / settings.MaximumHeight * maxLevel;
            var delta = new float[heights.Length];
            var lowerNeighbors = new int[6];
            var transfers = new float[6];

            for (var iteration = 0; iteration < settings.ThermalErosionIterations; iteration++)
            {
                Array.Clear(delta, 0, delta.Length);
                for (var index = 0; index < heights.Length; index++)
                {
                    if (!land[index])
                        continue;

                    var lowerCount = 0;
                    for (var direction = 0; direction < 6; direction++)
                    {
                        var neighbor = neighborIndices[index * 6 + direction];
                        if (neighbor >= 0 && land[neighbor] && heights[index] - heights[neighbor] > talusInLevels)
                            lowerNeighbors[lowerCount++] = neighbor;
                    }

                    if (lowerCount == 0)
                        continue;

                    for (var neighborIndex = 0; neighborIndex < lowerCount; neighborIndex++)
                    {
                        var neighbor = lowerNeighbors[neighborIndex];
                        var difference = heights[index] - heights[neighbor] - talusInLevels;
                        var transfer = Mathf.Min(difference * settings.ErosionRate / lowerCount, difference * 0.5f);
                        transfers[neighborIndex] = transfer;
                    }

                    for (var neighborIndex = 0; neighborIndex < lowerCount; neighborIndex++)
                    {
                        var neighbor = lowerNeighbors[neighborIndex];
                        var transfer = transfers[neighborIndex];
                        delta[index] -= transfer;
                        delta[neighbor] += transfer;
                    }
                }

                for (var index = 0; index < heights.Length; index++)
                {
                    if (land[index])
                        heights[index] = Mathf.Clamp(heights[index] + delta[index], 1f, maxLevel);
                }
            }
        }

        private static long CoordinateKey(int q, int r)
        {
            return ((long)q << 32) | (uint)r;
        }

        private static HexIslandMap SaveMap(IslandGenerationProfile profile, GenerationSettings settings, HexIslandCell[] cells)
        {
            var map = profile.LastGeneratedMap;
            var isNewAsset = map == null || !AssetDatabase.Contains(map);
            var path = map != null && AssetDatabase.Contains(map)
                ? AssetDatabase.GetAssetPath(map)
                : GetNewMapPath(profile);

            var folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureAssetFolder(folder);

            if (!isNewAsset)
                RemovePersistedPreview(path);

            var existingPreview = map != null ? map.Preview : null;
            if (!isNewAsset)
            {
                Undo.RecordObject(map, "Regenerate hex island");
            }

            var pixels = CreatePreviewPixels(settings, cells, profile.HeightGradient, out var previewWidth, out var previewHeight);
            var preview = existingPreview;
            if (preview == null)
                preview = new Texture2D(previewWidth, previewHeight, TextureFormat.RGBA32, false);
            else if (preview.width != previewWidth || preview.height != previewHeight)
                preview.Reinitialize(previewWidth, previewHeight, TextureFormat.RGBA32, false);

            preview.name = $"{profile.name} Preview";
            preview.hideFlags = HideFlags.HideAndDontSave;
            preview.filterMode = FilterMode.Point;
            preview.SetPixels32(pixels);
            preview.Apply(false, false);

            if (isNewAsset)
            {
                map = ScriptableObject.CreateInstance<HexIslandMap>();
                map.name = Path.GetFileNameWithoutExtension(path);
            }

            map.SetGeneratedData(
                settings.Columns,
                settings.Rows,
                settings.Seed,
                settings.HeightLevels,
                settings.MaximumHeight,
                cells,
                preview);

            if (isNewAsset)
                AssetDatabase.CreateAsset(map, path);

            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            return map;
        }

        private static void RemovePersistedPreview(string assetPath)
        {
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (var i = 0; i < subAssets.Length; i++)
            {
                var subAsset = subAssets[i];
                if (!(subAsset is Texture2D) || !subAsset.name.EndsWith(" Preview", StringComparison.Ordinal))
                    continue;

                AssetDatabase.RemoveObjectFromAsset(subAsset);
                UnityEngine.Object.DestroyImmediate(subAsset, true);
            }
        }

        public static void RefreshPreviewAppearance(HexIslandMap map, Gradient heightGradient)
        {
            if (map == null)
                return;

            var settings = new GenerationSettings
            {
                Columns = map.Columns,
                Rows = map.Rows,
                MaximumHeight = map.MaximumHeight
            };
            var pixels = CreatePreviewPixels(
                settings,
                map.Cells,
                heightGradient,
                out var previewWidth,
                out var previewHeight);
            var preview = map.Preview;
            if (preview == null)
                preview = new Texture2D(previewWidth, previewHeight, TextureFormat.RGBA32, false);
            else if (preview.width != previewWidth || preview.height != previewHeight)
                preview.Reinitialize(previewWidth, previewHeight, TextureFormat.RGBA32, false);

            preview.name = $"{map.name} Preview";
            preview.hideFlags = HideFlags.HideAndDontSave;
            preview.filterMode = FilterMode.Point;
            preview.SetPixels32(pixels);
            preview.Apply(false, false);
            map.SetPreviewTexture(preview);
        }

        private static string GetNewMapPath(IslandGenerationProfile profile)
        {
            var outputFolder = "Assets/GeneratedHexIslands";
            EnsureAssetFolder(outputFolder);
            var profilePath = AssetDatabase.GetAssetPath(profile);
            var guid = AssetDatabase.AssetPathToGUID(profilePath);
            var suffix = guid.Length >= 8 ? guid.Substring(0, 8) : Guid.NewGuid().ToString("N").Substring(0, 8);
            var fileName = $"{SanitizeFileName(profile.name)}_{suffix}_Island.asset";
            var path = $"{outputFolder}/{fileName}";
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) == null
                ? path
                : AssetDatabase.GenerateUniqueAssetPath(path);
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
                return;

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var name = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                throw new InvalidOperationException($"Cannot create project asset folder: {folderPath}");

            EnsureAssetFolder(parent);
            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static string SanitizeFileName(string value)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            var characters = value.ToCharArray();
            for (var i = 0; i < characters.Length; i++)
            {
                if (Array.IndexOf(invalidCharacters, characters[i]) >= 0 || characters[i] == '/')
                    characters[i] = '_';
            }

            return new string(characters);
        }

        private static Color32[] CreatePreviewPixels(
            GenerationSettings settings,
            HexIslandCell[] cells,
            Gradient heightGradient,
            out int width,
            out int height)
        {
            var hexRadius = PreviewHexRadius;
            var columnSpacing = hexRadius * SqrtThree;
            var rowSpacing = hexRadius * 1.5f;
            width = Mathf.CeilToInt(
                PreviewPadding * 2f + (settings.Columns + 0.5f) * columnSpacing);
            height = Mathf.CeilToInt(
                PreviewPadding * 2f + hexRadius * 2f + (settings.Rows - 1) * rowSpacing);

            var background = new Color32(17, 24, 31, 255);
            var outline = new Color32(35, 43, 49, 255);
            var pixels = new Color32[width * height];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = background;

            var water = new Color32(31, 87, 122, 255);
            for (var index = 0; index < cells.Length; index++)
            {
                var cell = cells[index];
                var color = GetCellColor(settings, cell, water, heightGradient);

                var row = index / settings.Columns;
                var column = index - row * settings.Columns;
                var axialR = row - settings.Rows / 2;
                var centerX = PreviewPadding + hexRadius * HalfSqrtThree + column * columnSpacing + (axialR & 1) * hexRadius * HalfSqrtThree;
                var centerY = height - PreviewPadding - hexRadius - row * rowSpacing;
                var minX = Mathf.Max(0, Mathf.FloorToInt(centerX - hexRadius));
                var maxX = Mathf.Min(width - 1, Mathf.CeilToInt(centerX + hexRadius));
                var minY = Mathf.Max(0, Mathf.FloorToInt(centerY - hexRadius));
                var maxY = Mathf.Min(height - 1, Mathf.CeilToInt(centerY + hexRadius));
                var innerRadius = Mathf.Max(1f, hexRadius - 1f);

                for (var y = minY; y <= maxY; y++)
                {
                    for (var x = minX; x <= maxX; x++)
                    {
                        var dx = Mathf.Abs(x + 0.5f - centerX);
                        var dy = Mathf.Abs(y + 0.5f - centerY);
                        if (!InsidePointyHex(dx, dy, hexRadius))
                            continue;

                        var pixelColor = InsidePointyHex(dx, dy, innerRadius) ? color : outline;
                        pixels[(height - 1 - y) * width + x] = pixelColor;
                    }
                }
            }

            return pixels;
        }

        private static Color32 GetCellColor(
            GenerationSettings settings,
            HexIslandCell cell,
            Color32 water,
            Gradient heightGradient)
        {
            if (!cell.IsLand)
                return water;

            var normalizedHeight = settings.MaximumHeight > 0f ? cell.Height / settings.MaximumHeight : 0f;
            return heightGradient != null
                ? heightGradient.Evaluate(Mathf.Clamp01(normalizedHeight))
                : new Color(0.35f, 0.55f, 0.3f, 1f);
        }

        private static bool InsidePointyHex(float dx, float dy, float radius)
        {
            return dx <= radius * HalfSqrtThree && dy + dx * InverseSqrtThree <= radius;
        }
    }
}
#endif
