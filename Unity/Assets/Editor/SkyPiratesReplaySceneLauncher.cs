using System;
using System.IO;
using SkyPirates.Local.Entry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkyPirates.Editor
{
    [InitializeOnLoad]
    internal static class SkyPiratesReplaySceneLauncher
    {
        private const string ReplayScenePath = "Assets/Scenes/ReplayScene.unity";
        private const string PendingReplayPathKey = "SkyPirates.ReplayScene.PendingReplayPath";

        static SkyPiratesReplaySceneLauncher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void OpenAndLoad(string replayPath)
        {
            if (!File.Exists(replayPath))
            {
                EditorUtility.DisplayDialog("Replay", "The selected replay file does not exist.", "OK");
                return;
            }

            try
            {
                Scene scene = EditorSceneManager.OpenScene(ReplayScenePath, OpenSceneMode.Single);
                if (!scene.IsValid())
                    throw new InvalidOperationException($"Could not open replay scene '{ReplayScenePath}'.");

                SessionState.SetString(PendingReplayPathKey, Path.GetFullPath(replayPath));
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                SessionState.EraseString(PendingReplayPathKey);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Replay", exception.Message, "OK");
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (!string.IsNullOrEmpty(SessionState.GetString(PendingReplayPathKey, string.Empty)))
                    EditorApplication.update += LoadPendingReplay;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update -= LoadPendingReplay;
                SessionState.EraseString(PendingReplayPathKey);
            }
        }

        private static void LoadPendingReplay()
        {
            string replayPath = SessionState.GetString(PendingReplayPathKey, string.Empty);
            if (string.IsNullOrEmpty(replayPath))
            {
                EditorApplication.update -= LoadPendingReplay;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= LoadPendingReplay;
                return;
            }

            LocalStart? localStart = UnityEngine.Object.FindAnyObjectByType<LocalStart>();
            if (localStart == null || !localStart.IsInitialized)
                return;

            EditorApplication.update -= LoadPendingReplay;
            SessionState.EraseString(PendingReplayPathKey);
            try
            {
                localStart.LoadReplayFile(replayPath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Replay", exception.Message, "OK");
            }
        }
    }
}
