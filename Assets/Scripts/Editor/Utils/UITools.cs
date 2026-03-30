using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DVG.Editor.Tools
{
    public static class UITools
    {
        private static int _changeCounter;

        [MenuItem("GameObject/DVG/UI/Fix Raycast Target", true)]
        [MenuItem("GameObject/DVG/UI/Disable Children Masking", true)]
        [MenuItem("GameObject/DVG/UI/Enable Children Masking", true)]
        private static bool ValidateTransform() => (Selection.activeTransform as RectTransform) != null;
        private static void LogChangedElements() => Debug.Log($"Changed {_changeCounter} elements");

        [MenuItem("GameObject/DVG/UI/Fix Raycast Target", false, 0)]
        public static void FixRaycastTarget(MenuCommand cmd)
        {
            GameObject selected = cmd.context as GameObject;
            if (selected == null)
                return;

            _changeCounter = 0;
            foreach (var item in selected.GetComponentsInChildren<Graphic>(true))
            {
                var value = item.raycastTarget;
                var target = item.TryGetComponent<IEventSystemHandler>(out _);
                if (target != value)
                {
                    Undo.RecordObject(item, nameof(FixRaycastTarget));
                    item.raycastTarget = target;
                    _changeCounter++;
                }
            }
            LogChangedElements();
        }

        [MenuItem("GameObject/DVG/UI/Enable Children Masking", false, 0)]
        public static void EnableChildrenMasking(MenuCommand cmd)
        {
            GameObject selected = cmd.context as GameObject;
            if (selected == null)
                return;

            _changeCounter = 0;
            EnableMasking(selected.transform);

            LogChangedElements();
        }

        [MenuItem("GameObject/DVG/UI/Disable Children Masking", false, 0)]
        public static void DisableChildrenMasking(MenuCommand cmd)
        {
            GameObject selected = cmd.context as GameObject;
            if (selected == null)
                return;

            _changeCounter = 0;
            DisableMaskingRecursive(selected.transform);

            LogChangedElements();
        }


        private static void EnableMasking(Transform go)
        {
            foreach (var item in go.GetComponentsInChildren<MaskableGraphic>(true))
            {
                if (item.transform == go)
                    continue;

                if (!item.maskable)
                {
                    Undo.RecordObject(item, nameof(EnableChildrenMasking));
                    item.maskable = true;
                    _changeCounter++;
                }
            }
        }

        private static void DisableMaskingRecursive(Transform go)
        {
            if (go.TryGetComponent<MaskableGraphic>(out var graphic) && graphic.maskable)
            {
                Undo.RecordObject(graphic, nameof(DisableChildrenMasking));
                graphic.maskable = false;
                _changeCounter++;
            }

            if (go.TryGetComponent<Mask>(out _))
                return;

            var count = go.childCount;
            for (int i = 0; i < count; i++)
            {
                DisableMaskingRecursive(go.GetChild(i));
            }
        }
    }
}
