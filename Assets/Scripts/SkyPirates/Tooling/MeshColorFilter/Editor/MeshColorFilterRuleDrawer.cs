using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DVG.SkyPirates.Tooling.MeshColorFilter.Editor
{
    [CustomPropertyDrawer(typeof(MeshColorFilterRule))]
    public sealed class MeshColorFilterRuleDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty enabled = property.FindPropertyRelative("enabled");
            SerializedProperty zoneName = property.FindPropertyRelative("zoneName");
            SerializedProperty material = property.FindPropertyRelative("material");
            SerializedProperty mode = property.FindPropertyRelative("mode");

            var root = new VisualElement();
            var header = new VisualElement();
            header.AddToClassList("rule-header");
            var enabledToggle = new Toggle("Enabled");
            enabledToggle.BindProperty(enabled);
            var title = new Label(GetRuleTitle(zoneName));
            title.AddToClassList("rule-title");
            header.Add(enabledToggle);
            header.Add(title);
            root.Add(header);

            Foldout foldout = new Foldout
            {
                text = "Settings",
                value = true
            };
            foldout.Add(new PropertyField(zoneName, "Zone"));
            foldout.Add(new PropertyField(material, "Material"));

            PropertyField modeField = new PropertyField(mode, "Filter");
            foldout.Add(modeField);

            VisualElement rgbFields = new VisualElement();
            rgbFields.Add(new PropertyField(property.FindPropertyRelative("targetColor"), "Target RGB"));
            rgbFields.Add(CreateSlider(property.FindPropertyRelative("rgbTolerance"), "RGB distance", 0f, 1.732f));
            foldout.Add(rgbFields);

            VisualElement hsvFields = new VisualElement();
            hsvFields.Add(new PropertyField(property.FindPropertyRelative("hsvBaseColor"), "Hue base color"));
            hsvFields.Add(CreateSlider(property.FindPropertyRelative("hueTolerance"), "Hue range (± degrees)", 0f, 180f));
            hsvFields.Add(CreateSlider(property.FindPropertyRelative("saturationMin"), "Saturation min", 0f, 1f));
            hsvFields.Add(CreateSlider(property.FindPropertyRelative("saturationMax"), "Saturation max", 0f, 1f));
            hsvFields.Add(CreateSlider(property.FindPropertyRelative("valueMin"), "Value min", 0f, 1f));
            hsvFields.Add(CreateSlider(property.FindPropertyRelative("valueMax"), "Value max", 0f, 1f));
            foldout.Add(hsvFields);

            void UpdateFilterFields()
            {
                bool useHsv = mode.enumValueIndex == (int)MeshColorFilterMode.HsvRange;
                rgbFields.style.display = useHsv ? DisplayStyle.None : DisplayStyle.Flex;
                hsvFields.style.display = useHsv ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateFilterFields();
            modeField.TrackPropertyValue(mode, _ => UpdateFilterFields());
            root.TrackPropertyValue(zoneName, _ => title.text = GetRuleTitle(zoneName));
            root.Add(foldout);
            return root;
        }

        private static Slider CreateSlider(SerializedProperty property, string label, float minimum, float maximum)
        {
            var slider = new Slider(label, minimum, maximum)
            {
                showInputField = true,
                bindingPath = property.propertyPath
            };
            slider.BindProperty(property);
            return slider;
        }

        private static string GetRuleTitle(SerializedProperty zoneName)
        {
            return string.IsNullOrEmpty(zoneName.stringValue) ? "Rule" : "Rule — " + zoneName.stringValue;
        }
    }
}
