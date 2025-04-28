using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DVG.Json.Editor
{
    [CustomEditor(typeof(TextAsset))]
    public class JsonEditorWindow : UnityEditor.Editor
    {
        private string JsonPath => AssetDatabase.GetAssetPath(target);
        private string JsonGuid => AssetDatabase.AssetPathToGUID(JsonPath);
        private bool IsCompatible => JsonPath.EndsWith(".json");

        private readonly List<Type> _jsonAssetTypes = new();
        private readonly List<string> _jsonAssetNames = new();
        private readonly List<Type> _wrapperTypes = new();

        private IScriptableWrapper _objectWrapper;
        private int _selectedTypeIndex = 0;


        private void OnEnable()
        {
            if (!IsCompatible)
                return;
            UpdateJsonAssetTypes();
            if (JsonTypeCache.Instance.TryGetType(JsonGuid, out var typeName))
                _selectedTypeIndex = _jsonAssetNames.IndexOf(typeName);
            else
                _selectedTypeIndex = 0;
        }

        private void OnDisable()
        {
            WriteJson();
        }

        protected override void OnHeaderGUI()
        {
            if (!IsCompatible)
                base.OnHeaderGUI();
        }

        public override void OnInspectorGUI()
        {
            if (!IsCompatible)
                base.OnInspectorGUI();
            else
            {
                var guiState = GUI.enabled;
                GUI.enabled = true;
                JsonInspectorGUI();
                GUI.enabled = guiState;
            }
        }


        public override bool UseDefaultMargins()
        {
            return IsCompatible || base.UseDefaultMargins();
        }
        private void JsonInspectorGUI()
        {
            EditorGUILayout.Space();
            var prevIndex = _selectedTypeIndex;
            _selectedTypeIndex = EditorGUILayout.Popup("Object type", _selectedTypeIndex, _jsonAssetNames.ToArray());
            if (_selectedTypeIndex != prevIndex)
                JsonTypeCache.Instance.SetType(JsonGuid, _jsonAssetNames[_selectedTypeIndex]);

            _objectWrapper = prevIndex != _selectedTypeIndex || _objectWrapper == null ? CreateWrapper() : _objectWrapper;
            if (_objectWrapper == null)
                return;

            _objectWrapper.Draw("Value");
            EditorGUILayout.Space();
            if (GUILayout.Button("Save", GUILayout.Width(300)))
                WriteJson();
        }

        private void WriteJson()
        {
            if (_objectWrapper == null || !File.Exists(JsonPath))
                return;
            var json = JsonConvert.SerializeObject(_objectWrapper.Value, Formatting.Indented, DebugConfigSerializerSettings.Instance);
            File.WriteAllText(JsonPath, json);
            AssetDatabase.ImportAsset(JsonPath);
        }


        private void UpdateJsonAssetTypes()
        {
            _jsonAssetTypes.Clear();
            _jsonAssetNames.Clear();
            _wrapperTypes.Clear();

            var types = TypeCache.GetTypesWithAttribute<JsonAssetAttribute>();
            var wrapperTypes = TypeCache.GetTypesDerivedFrom<IScriptableWrapper>();

            _jsonAssetTypes.Add(null);
            _jsonAssetNames.Add("None");

            _jsonAssetTypes.AddRange(types);
            _jsonAssetNames.AddRange(types.Select(t => t.FullName));
            _wrapperTypes.AddRange(wrapperTypes);
        }

        private IScriptableWrapper CreateWrapper()
        {
            if (string.IsNullOrWhiteSpace(JsonPath) || !File.Exists(JsonPath))
                return null;
            if (_jsonAssetTypes.Count <= _selectedTypeIndex || _selectedTypeIndex < 0)
                return null;

            var type = _jsonAssetTypes[_selectedTypeIndex];
            Type found = _wrapperTypes.FirstOrDefault(t =>
            {
                var args = t.BaseType.GetGenericArguments();
                return args.Length == 1 && args[0] == type;
            });

            if (found == null)
            {
                return null;
            }

            var wrapper = CreateInstance(found) as IScriptableWrapper;
            var json = File.ReadAllText(JsonPath);
            object content = null;
            try
            {
                content = JsonConvert.DeserializeObject(json, type);
                wrapper.Value = content;
            }
            catch { }
            return wrapper;
        }


        [MenuItem("Assets/Create/Json", priority = 0)]
        public static void CreateNewJsonFile()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            if (path.Contains("."))
                path = path.Remove(path.LastIndexOf('/'));
            path = Path.Combine(path, "New Json File.json");
            ProjectWindowUtil.CreateAssetWithContent(path, string.Empty);
        }
    }
}
