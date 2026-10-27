#if UNITY_EDITOR
using UnityEditor;

namespace SlyPirates.Tooling.Editor
{
    internal static class SkyPiratesConfigMenu
    {
        private const string ConfigPath = "Assets/Settings/Configs/SkyPiratesGoogleSheets.asset";

        [MenuItem("SkyPirates/Configs")]
        private static void OpenConfigs()
        {
            var config = AssetDatabase.LoadAssetAtPath<GoogleSheetsConfigLoader>(ConfigPath);
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }
}
#endif
