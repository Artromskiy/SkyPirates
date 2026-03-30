#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DVG
{
    [CustomPropertyDrawer(typeof(fix))]
    public class FixDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Float, label);
            var rawProp = property.FindPropertyRelative("raw");
            EditorGUI.BeginChangeCheck();
            float value = (float)new fix(rawProp.intValue);
            float newValue = EditorGUI.FloatField(position, label, value);
            if (EditorGUI.EndChangeCheck())
            {
                rawProp.intValue = ((fix)newValue).raw;
            }
        }
    }

    [CustomPropertyDrawer(typeof(fix2))]
    public class Fix2Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector2, label);
            var xProp = property.FindPropertyRelative("x.raw");
            var yProp = property.FindPropertyRelative("y.raw");
            var value = new Vector2(
                (float)new fix(xProp.intValue),
                (float)new fix(yProp.intValue)
            );

            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.Vector2Field(position, label, value);

            if (EditorGUI.EndChangeCheck())
            {
                xProp.intValue = ((fix)newValue.x).raw;
                yProp.intValue = ((fix)newValue.y).raw;
            }
        }
    }

    [CustomPropertyDrawer(typeof(fix3))]
    public class Fix3Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, label);
            var xProp = property.FindPropertyRelative("x.raw");
            var yProp = property.FindPropertyRelative("y.raw");
            var zProp = property.FindPropertyRelative("z.raw");
            var value = new Vector3(
                (float)new fix(xProp.intValue),
                (float)new fix(yProp.intValue),
                (float)new fix(zProp.intValue)
            );

            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.Vector3Field(position, label, value);

            if (EditorGUI.EndChangeCheck())
            {
                xProp.intValue = ((fix)newValue.x).raw;
                yProp.intValue = ((fix)newValue.y).raw;
                zProp.intValue = ((fix)newValue.z).raw;
            }
        }
    }

    [CustomPropertyDrawer(typeof(fix4))]
    public class Fix4Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector4, label);
            var xProp = property.FindPropertyRelative("x.raw");
            var yProp = property.FindPropertyRelative("y.raw");
            var zProp = property.FindPropertyRelative("z.raw");
            var wProp = property.FindPropertyRelative("w.raw");
            var value = new Vector4(
                (float)new fix(xProp.intValue),
                (float)new fix(yProp.intValue),
                (float)new fix(zProp.intValue),
                (float)new fix(wProp.intValue)
            );

            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.Vector4Field(position, label, value);

            if (EditorGUI.EndChangeCheck())
            {
                xProp.intValue = ((fix)newValue.x).raw;
                yProp.intValue = ((fix)newValue.y).raw;
                zProp.intValue = ((fix)newValue.z).raw;
                wProp.intValue = ((fix)newValue.w).raw;
            }
        }
    }
}
#endif