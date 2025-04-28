using DVG.Editor.Configs;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DVG.Json.Editor
{
    [CreateAssetMenu(fileName = nameof(JsonTypeCache), menuName = "Tools/" + nameof(JsonTypeCache))]
    public class JsonTypeCache : SingletonConfig<JsonTypeCache>
    {
        [SerializeField]
        private List<JsonToType> _jsonGuidToType = new();
        private const string NoneType = "None";

        private void OnEnable()
        {
            HashSet<string> set = new();
            List<JsonToType> newJsonGuidToType = new();
            foreach (var item in _jsonGuidToType)
            {
                if (item.type != NoneType && set.Add(item.jsonGuid) && !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(item.jsonGuid)))
                    newJsonGuidToType.Add(item);
            }
            _jsonGuidToType = newJsonGuidToType;
        }

        public bool TryGetType(string jsonGuid, out string result)
        {
            result = _jsonGuidToType.Find(v => v.jsonGuid == jsonGuid).type;
            return !string.IsNullOrEmpty(result);
        }

        public void SetType(string jsonGuid, string type)
        {
            if (type == "None")
                RemoveGuid(jsonGuid);
            else
            {
                var index = _jsonGuidToType.FindIndex(v => v.jsonGuid == jsonGuid);
                if (index >= 0)
                    _jsonGuidToType[index] = new JsonToType(jsonGuid, type);
                else
                    _jsonGuidToType.Add(new JsonToType(jsonGuid, type));
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void RemoveGuid(string jsonGuid)
        {
            var index = _jsonGuidToType.FindIndex(v => v.jsonGuid == jsonGuid);
            if (index >= 0)
                _jsonGuidToType.RemoveAt(index);
        }


        [Serializable]
        private struct JsonToType
        {
            public string jsonGuid;
            public string type;

            public JsonToType(string jsonGuid, string type)
            {
                this.jsonGuid = jsonGuid;
                this.type = type;
            }
        }
    }
}
