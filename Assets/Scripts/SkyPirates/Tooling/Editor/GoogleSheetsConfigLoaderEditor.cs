#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DVG.SkyPirates.Tooling.Editor
{
    [CustomEditor(typeof(GoogleSheetsConfigLoader))]
    public sealed class GoogleSheetsConfigLoaderEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var status = new VisualElement();
            status.style.display = DisplayStyle.None;
            var progress = new ProgressBar();
            progress.style.display = DisplayStyle.None;
            var button = new Button { text = "Synchronize" };
            button.clicked += async () =>
            {
                serializedObject.ApplyModifiedProperties();
                button.SetEnabled(false);
                var loader = (GoogleSheetsConfigLoader)target;
                var totalSheetCount = loader.Sheets?.Count ?? 0;
                progress.title = $"Loaded sheets: 0/{totalSheetCount}";
                progress.value = 0;
                progress.style.display = DisplayStyle.Flex;
                ShowStatus(status, "Loading sheets…", HelpBoxMessageType.Info);

                try
                {
                    var loadedSheetCount = await GoogleSheetsConfigSync.Sync(loader, (loaded, total) =>
                    {
                        progress.title = $"Loaded sheets: {loaded}/{total}";
                        progress.value = total == 0 ? 0 : loaded * 100f / total;
                    });
                    progress.title = $"Loaded sheets: {loadedSheetCount}";
                    progress.value = 100;
                    ShowStatus(status, $"Loaded {loadedSheetCount} sheets.", HelpBoxMessageType.Info);
                }
                catch (Exception exception)
                {
                    ShowStatus(status, exception.Message, HelpBoxMessageType.Error);
                }
                finally
                {
                    progress.style.display = DisplayStyle.None;
                    button.SetEnabled(true);
                }
            };

            root.Add(button);
            root.Add(progress);
            root.Add(status);
            return root;
        }

        private static void ShowStatus(VisualElement container, string message, HelpBoxMessageType type)
        {
            container.Clear();
            container.Add(new HelpBox(message, type));
            container.style.display = DisplayStyle.Flex;
        }
    }
}
#endif
