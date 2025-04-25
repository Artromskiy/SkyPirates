using DVG.Core.Ids;
using System;
using UnityEditor;
using UnityEngine;

namespace DVG.Editor.CustomDrawers
{
    public abstract class PopupStringIdDrawer<TId> : PropertyDrawer
        where TId : struct, IStringId
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var data = (TId)property.boxedValue;
            var all = GetValues();
            var currentIndex = Array.IndexOf(all, data.Value);
            currentIndex = currentIndex < 0 ? 0 : currentIndex;

            var oldColor = GUI.color;
            if (!IsValid())
            {
                GUI.color = Color.red;
            }
            var rect = EditorGUI.PrefixLabel(position, label);
            var newIndex = EditorGUI.Popup(rect, currentIndex, all);
            property.boxedValue = Activator.CreateInstance(typeof(TId), new object[] { all[newIndex] });
            GUI.color = oldColor;
            EditorGUI.EndProperty();
        }

        protected virtual bool IsValid() => true;
        protected abstract string[] GetValues();
    }

    public abstract class SimpleStringIdDrawer<TId> : PropertyDrawer
        where TId : struct, IStringId
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var data = (TId)property.boxedValue;

            var oldColor = GUI.color;
            var rect = EditorGUI.PrefixLabel(position, label);
            string value = EditorGUI.TextField(rect, data.Value);
            property.boxedValue = Activator.CreateInstance(typeof(TId), new object[] { value });
            GUI.color = oldColor;
            EditorGUI.EndProperty();
        }
    }
}