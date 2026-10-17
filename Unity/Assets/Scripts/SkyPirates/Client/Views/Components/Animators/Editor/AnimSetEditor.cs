#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SlyPirates.Client.Views.Components.Animators.Editor
{
    [CustomEditor(typeof(AnimSet))]
    public sealed class AnimSetEditor : UnityEditor.Editor
    {
        private SerializedProperty _structureProfile = null!;
        private SerializedProperty _clips = null!;

        private void OnEnable()
        {
            _structureProfile = serializedObject.FindProperty("_structureProfile");
            _clips = serializedObject.FindProperty("_clips");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_structureProfile);
            bool profileChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            AnimProfile profile = _structureProfile.objectReferenceValue as AnimProfile;
            if (profile == null)
            {
                EditorGUILayout.HelpBox("Assign an AnimProfile to display its clip holders.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            SerializedObject profileObject = new SerializedObject(profile);
            profileObject.Update();
            SerializedProperty profileHolders = profileObject.FindProperty("_clipHolders");
            if (profileHolders == null)
            {
                EditorGUILayout.HelpBox("The assigned AnimProfile has no clip holder data.", MessageType.Warning);
                return;
            }

            if (profileChanged || NeedsSynchronization(profileHolders))
            {
                SynchronizeClipHolders(profileHolders);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Clip Holders", EditorStyles.boldLabel);
            if (profileHolders.arraySize == 0)
            {
                EditorGUILayout.HelpBox("The assigned AnimProfile does not define any clip holders.", MessageType.Info);
            }

            for (int i = 0; i < profileHolders.arraySize; i++)
            {
                SerializedProperty profileHolder = profileHolders.GetArrayElementAtIndex(i);
                SerializedProperty holderName = profileHolder.FindPropertyRelative("_name");
                SerializedProperty holderLayer = profileHolder.FindPropertyRelative("_layer");
                SerializedProperty clipDefinition = _clips.GetArrayElementAtIndex(i);
                SerializedProperty clip = clipDefinition.FindPropertyRelative("_clip");
                SerializedProperty speed = clipDefinition.FindPropertyRelative("_defaultSpeed");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Holder", holderName.stringValue);
                    EditorGUILayout.TextField("Layer", holderLayer.stringValue);
                }

                EditorGUILayout.PropertyField(clip, new GUIContent("Animation Clip"));
                EditorGUILayout.PropertyField(speed, new GUIContent("Default Speed"));
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool NeedsSynchronization(SerializedProperty profileHolders)
        {
            if (_clips.arraySize != profileHolders.arraySize)
            {
                return true;
            }

            for (int i = 0; i < profileHolders.arraySize; i++)
            {
                string profileName = profileHolders.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("_name").stringValue;
                string setName = _clips.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("_clipHolder").stringValue;
                if (profileName != setName)
                {
                    return true;
                }
            }

            return false;
        }

        private void SynchronizeClipHolders(SerializedProperty profileHolders)
        {
            var existing = new Dictionary<string, ClipValues>();
            for (int i = 0; i < _clips.arraySize; i++)
            {
                SerializedProperty definition = _clips.GetArrayElementAtIndex(i);
                string name = definition.FindPropertyRelative("_clipHolder").stringValue;
                if (string.IsNullOrEmpty(name) || existing.ContainsKey(name))
                {
                    continue;
                }

                existing.Add(name, new ClipValues(
                    definition.FindPropertyRelative("_clip").objectReferenceValue,
                    definition.FindPropertyRelative("_defaultSpeed").floatValue));
            }

            _clips.arraySize = profileHolders.arraySize;
            for (int i = 0; i < profileHolders.arraySize; i++)
            {
                string name = profileHolders.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("_name").stringValue;
                SerializedProperty definition = _clips.GetArrayElementAtIndex(i);
                definition.FindPropertyRelative("_clipHolder").stringValue = name;

                SerializedProperty clip = definition.FindPropertyRelative("_clip");
                SerializedProperty speed = definition.FindPropertyRelative("_defaultSpeed");
                if (existing.TryGetValue(name, out ClipValues values))
                {
                    clip.objectReferenceValue = values.Clip;
                    speed.floatValue = values.Speed;
                }
                else
                {
                    clip.objectReferenceValue = null;
                    speed.floatValue = 1f;
                }
            }
        }

        private readonly struct ClipValues
        {
            public readonly Object Clip;
            public readonly float Speed;

            public ClipValues(Object clip, float speed)
            {
                Clip = clip;
                Speed = speed;
            }
        }
    }
}
#endif
