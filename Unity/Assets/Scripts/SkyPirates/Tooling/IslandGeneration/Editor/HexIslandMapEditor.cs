#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DVG.SkyPirates.Tooling.IslandGeneration.Editor
{
    [CustomEditor(typeof(HexIslandMap))]
    public sealed class HexIslandMapEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var map = (HexIslandMap)target;
            var root = new VisualElement();
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Scripts/SkyPirates/Tooling/IslandGeneration/Editor/IslandGenerationWindow.uss");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            var title = new Label("Generated Hex Island");
            title.AddToClassList("window-title");
            root.Add(title);
            root.Add(new Label($"{map.Columns} × {map.Rows} hexes · {map.LandCellCount} land hexes · seed {map.Seed}"));
            if (map.Preview != null)
            {
                var preview = new Image { image = map.Preview, scaleMode = ScaleMode.ScaleToFit };
                preview.AddToClassList("preview-image");
                root.Add(preview);
            }

            return root;
        }
    }
}
#endif
