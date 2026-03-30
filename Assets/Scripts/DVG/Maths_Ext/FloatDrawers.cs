#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace DVG
{
    [CustomPropertyDrawer(typeof(float2))]
    public class Float2Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector2, label);
            var xProp = property.FindPropertyRelative("x");
            var yProp = property.FindPropertyRelative("y");
            var value = new Vector2(
                xProp.floatValue,
                yProp.floatValue
            );

            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.Vector2Field(position, label, value);

            if (EditorGUI.EndChangeCheck())
            {
                xProp.floatValue = newValue.x;
                yProp.floatValue = newValue.y;
            }
        }
    }

    [CustomPropertyDrawer(typeof(float3))]
    public class Float3Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, label);
            var xProp = property.FindPropertyRelative("x");
            var yProp = property.FindPropertyRelative("y");
            var zProp = property.FindPropertyRelative("z");
            var value = new Vector3(
                xProp.floatValue,
                yProp.floatValue,
                zProp.floatValue
            );

            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.Vector3Field(position, label, value);

            if (EditorGUI.EndChangeCheck())
            {
                xProp.floatValue = newValue.x;
                yProp.floatValue = newValue.y;
                zProp.floatValue = newValue.z;
            }
        }
    }

    [CustomPropertyDrawer(typeof(float4))]
    public class Float4Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector4, label);
            var xProp = property.FindPropertyRelative("x");
            var yProp = property.FindPropertyRelative("y");
            var zProp = property.FindPropertyRelative("z");
            var wProp = property.FindPropertyRelative("w");
            var value = new Vector4(
                xProp.floatValue,
                yProp.floatValue,
                zProp.floatValue,
                wProp.floatValue
            );

            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.Vector4Field(position, label, value);

            if (EditorGUI.EndChangeCheck())
            {
                xProp.floatValue = newValue.x;
                yProp.floatValue = newValue.y;
                zProp.floatValue = newValue.z;
                wProp.floatValue = newValue.w;
            }
        }
    }
}
#endif