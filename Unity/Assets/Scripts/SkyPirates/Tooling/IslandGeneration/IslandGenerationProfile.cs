using UnityEngine;

namespace DVG.SkyPirates.Tooling.IslandGeneration
{
    [CreateAssetMenu(
        fileName = "IslandGenerationProfile",
        menuName = "Sky Pirates/Tooling/Island Generation Profile")]
    public sealed class IslandGenerationProfile : ScriptableObject
    {
        private const int DefaultColumns = 81;
        private const int DefaultRows = 61;
        private const int DefaultSeed = 12345;
        private const float DefaultIslandScale = 0.78f;
        private const float DefaultOutlinePower = 2f;
        private const float DefaultBoundaryWarp = 0.2f;
        private const float DefaultBoundaryFrequency = 3f;
        private const float DefaultMaximumHeight = 24f;
        private const int DefaultHeightLevels = 8;
        private const float DefaultPreviewVerticalScale = 1f;
        private const float DefaultCoastExponent = 1.4f;
        private const float DefaultHeightNoiseStrength = 0.12f;
        private const float DefaultHeightNoiseFrequency = 5f;
        private const int DefaultThermalErosionIterations = 8;
        private const float DefaultTalusHeight = 2f;
        private const float DefaultErosionRate = 0.25f;

        private static Gradient CreateDefaultHeightGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.72f, 0.64f, 0.42f), 0f),
                    new GradientColorKey(new Color(0.35f, 0.55f, 0.3f), 0.18f),
                    new GradientColorKey(new Color(0.42f, 0.4f, 0.34f), 0.62f),
                    new GradientColorKey(new Color(0.91f, 0.91f, 0.86f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return gradient;
        }

        [Header("Hex grid")]
        [SerializeField, Min(16)] private int columns = DefaultColumns;
        [SerializeField, Min(16)] private int rows = DefaultRows;
        [SerializeField] private int seed = DefaultSeed;

        [Header("Island shape")]
        [SerializeField, Range(0.1f, 0.98f)] private float islandScale = DefaultIslandScale;
        [SerializeField, Range(1f, 8f)] private float outlinePower = DefaultOutlinePower;
        [SerializeField, Range(0f, 0.45f)] private float boundaryWarp = DefaultBoundaryWarp;
        [SerializeField, Range(0.5f, 16f)] private float boundaryFrequency = DefaultBoundaryFrequency;
        [SerializeField, Range(-0.5f, 0.5f)] private float centerOffsetX;
        [SerializeField, Range(-0.5f, 0.5f)] private float centerOffsetY;

        [Header("Height field")]
        [SerializeField, Min(1f)] private float maximumHeight = DefaultMaximumHeight;
        [SerializeField, Range(2, 32)] private int heightLevels = DefaultHeightLevels;
        [SerializeField, Range(0.25f, 4f)] private float coastExponent = DefaultCoastExponent;
        [SerializeField, Range(0f, 0.5f)] private float heightNoiseStrength = DefaultHeightNoiseStrength;
        [SerializeField, Range(0.25f, 16f)] private float heightNoiseFrequency = DefaultHeightNoiseFrequency;

        [Header("Thermal erosion")]
        [SerializeField, Range(0, 32)] private int thermalErosionIterations = DefaultThermalErosionIterations;
        [SerializeField, Min(0f)] private float talusHeight = DefaultTalusHeight;
        [SerializeField, Range(0f, 1f)] private float erosionRate = DefaultErosionRate;

        [Header("Preview")]
        [SerializeField] private Gradient heightGradient = CreateDefaultHeightGradient();
        [SerializeField, Range(0.1f, 5f)] private float previewVerticalScale = DefaultPreviewVerticalScale;

        [SerializeField, HideInInspector] private HexIslandMap lastGeneratedMap;

        public int Columns => columns;
        public int Rows => rows;
        public int Seed => seed;
        public float IslandScale => islandScale;
        public float OutlinePower => outlinePower;
        public float BoundaryWarp => boundaryWarp;
        public float BoundaryFrequency => boundaryFrequency;
        public float CenterOffsetX => centerOffsetX;
        public float CenterOffsetY => centerOffsetY;
        public float MaximumHeight => maximumHeight;
        public int HeightLevels => heightLevels;
        public float CoastExponent => coastExponent;
        public float HeightNoiseStrength => heightNoiseStrength;
        public float HeightNoiseFrequency => heightNoiseFrequency;
        public int ThermalErosionIterations => thermalErosionIterations;
        public float TalusHeight => talusHeight;
        public float ErosionRate => erosionRate;
        public Gradient HeightGradient => heightGradient;
        public float PreviewVerticalScale => previewVerticalScale;

        public void ResetSettingsToDefaults()
        {
            columns = DefaultColumns;
            rows = DefaultRows;
            seed = DefaultSeed;
            islandScale = DefaultIslandScale;
            outlinePower = DefaultOutlinePower;
            boundaryWarp = DefaultBoundaryWarp;
            boundaryFrequency = DefaultBoundaryFrequency;
            centerOffsetX = 0f;
            centerOffsetY = 0f;
            maximumHeight = DefaultMaximumHeight;
            heightLevels = DefaultHeightLevels;
            coastExponent = DefaultCoastExponent;
            heightNoiseStrength = DefaultHeightNoiseStrength;
            heightNoiseFrequency = DefaultHeightNoiseFrequency;
            thermalErosionIterations = DefaultThermalErosionIterations;
            talusHeight = DefaultTalusHeight;
            erosionRate = DefaultErosionRate;
            heightGradient = CreateDefaultHeightGradient();
            previewVerticalScale = DefaultPreviewVerticalScale;
        }

        public HexIslandMap LastGeneratedMap
        {
            get => lastGeneratedMap;
            set => lastGeneratedMap = value;
        }
    }
}
