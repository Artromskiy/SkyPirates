using System;
using System.Collections.Generic;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.MeshColorFilter.Editor
{
    public enum MeshColorFilterMode
    {
        RgbDistance,
        HsvRange
    }

    public enum MeshTriangleAssignmentMode
    {
        AverageColor,
        MajorityVertices,
        AdaptiveSubdivision
    }

    [CreateAssetMenu(menuName = "Sky Pirates/Tooling/Mesh Color Filter Profile", fileName = "MeshColorFilterProfile")]
    public sealed class MeshColorFilterProfile : ScriptableObject
    {
        [SerializeField] private List<MeshColorFilterRule> rules = new List<MeshColorFilterRule>();
        [SerializeField] private bool previewExpanded = true;
        [SerializeField] private MeshTriangleAssignmentMode triangleMode = MeshTriangleAssignmentMode.AverageColor;
        [SerializeField, Range(1, 5)] private int adaptiveSplitDepth = 3;

        public List<MeshColorFilterRule> Rules => rules;
        public MeshTriangleAssignmentMode TriangleMode { get => triangleMode; set => triangleMode = value; }
        public int AdaptiveSplitDepth { get => Mathf.Clamp(adaptiveSplitDepth, 1, 5); set => adaptiveSplitDepth = Mathf.Clamp(value, 1, 5); }
    }

    [Serializable]
    public sealed class MeshColorFilterRule
    {
        [SerializeField] private bool settingsExpanded = true;
        [SerializeField] private bool enabled = true;
        [SerializeField] private string zoneName = "Zone";
        [SerializeField] private Material material;
        [SerializeField] private MeshColorFilterMode mode;
        [SerializeField] private Color targetColor = Color.white;
        [SerializeField, Range(0f, 1.732f)] private float rgbTolerance = 0.1f;
        [SerializeField] private Color hsvBaseColor = Color.red;
        [SerializeField, Range(0f, 180f)] private float hueTolerance = 15f;
        [SerializeField, Range(0f, 1f)] private float saturationMin;
        [SerializeField, Range(0f, 1f)] private float saturationMax = 1f;
        [SerializeField, Range(0f, 1f)] private float valueMin;
        [SerializeField, Range(0f, 1f)] private float valueMax = 1f;

        public bool Enabled { get => enabled; set => enabled = value; }
        public string ZoneName { get => zoneName; set => zoneName = value; }
        public Material Material { get => material; set => material = value; }
        public MeshColorFilterMode Mode { get => mode; set => mode = value; }
        public Color TargetColor { get => targetColor; set => targetColor = value; }
        public float RgbTolerance { get => rgbTolerance; set => rgbTolerance = value; }
        public Color HsvBaseColor { get => hsvBaseColor; set => hsvBaseColor = value; }
        public float HueTolerance { get => hueTolerance; set => hueTolerance = Mathf.Clamp(value, 0f, 180f); }
        public float SaturationMin { get => saturationMin; set => saturationMin = value; }
        public float SaturationMax { get => saturationMax; set => saturationMax = value; }
        public float ValueMin { get => valueMin; set => valueMin = value; }
        public float ValueMax { get => valueMax; set => valueMax = value; }
    }
}
