using UnityEditor;
using UnityEngine;

namespace DVG.Editor.Tools
{
    public static class PrefabTools
    {
        [MenuItem("Assets/DVG/Remove Missing Scripts", false, 0)]
        public static void Remove()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
                return;

            Delta.Diagnostics.Trace.Info($"Selected {selected.Length} elements");

            int counter = 0;
            foreach (var item in selected)
            {
                counter += RemoveMissingScriptsRecursive(item);
            }

            Delta.Diagnostics.Trace.Info($"Removed {counter} elements");
            AssetDatabase.Refresh();
        }

        private static int RemoveMissingScriptsRecursive(GameObject root)
        {
            int removed = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            return removed;
        }
    }
}


