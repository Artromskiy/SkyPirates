using DVG.SkyPirates.Shared.Ids;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(TileId))]
[CustomPropertyDrawer(typeof(GoodsId))]
[CustomPropertyDrawer(typeof(CactusId))]
[CustomPropertyDrawer(typeof(TreeId))]
[CustomPropertyDrawer(typeof(RockId))]
[CustomPropertyDrawer(typeof(UnitId))]
public class IdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        SerializedProperty valueProp = property.FindPropertyRelative("Value");
        EditorGUI.BeginChangeCheck();
        var value = string.IsNullOrEmpty(valueProp.stringValue) ? string.Empty : valueProp.stringValue;
        string newValue = EditorGUI.TextField(position, label, value);
        if (EditorGUI.EndChangeCheck())
        {
            valueProp.stringValue = newValue;
        }
        EditorGUI.EndProperty();
    }
}