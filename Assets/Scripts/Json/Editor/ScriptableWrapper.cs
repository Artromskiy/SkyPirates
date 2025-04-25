using System;
using UnityEditor;
using UnityEngine;

namespace DVG.Json.Editor
{
    [Serializable]
    public class JsonWrapper<T> : ScriptableObject, IScriptableWrapper
    {
        [SerializeField]
        private T _value;

        private SerializedObject _serializedObject;
        private SerializedProperty _serializedProperty;

        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                SerializedObject.Update();
            }
        }

        private SerializedObject SerializedObject => _serializedObject ??= new SerializedObject(this);
        private SerializedProperty SerializedProperty => _serializedProperty ??= SerializedObject.FindProperty(nameof(_value));

        object IScriptableWrapper.Value
        {
            get => Value;
            set => Value = ((T)value) ?? Value;
        }

        public bool Draw(string name)
        {
            SerializedProperty.isExpanded = true;
            EditorGUILayout.PropertyField(SerializedProperty, new GUIContent(name), true);
            return SerializedObject.ApplyModifiedProperties();
        }
    }

    public interface IScriptableWrapper
    {
        bool Draw(string name);
        object Value { get; set; }
    }
}