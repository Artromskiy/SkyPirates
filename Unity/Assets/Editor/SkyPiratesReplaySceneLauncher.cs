using System;
using System.IO;
using DVG.SkyPirates.Local.Entry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DVG.SkyPirates.Editor
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

        public static void OpenAndPlay(string replayPath)
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
                    EditorApplication.update += StartPendingReplay;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update -= StartPendingReplay;
                SessionState.EraseString(PendingReplayPathKey);
            }
        }

        private static void StartPendingReplay()
        {
            string replayPath = SessionState.GetString(PendingReplayPathKey, string.Empty);
            if (string.IsNullOrEmpty(replayPath))
            {
                EditorApplication.update -= StartPendingReplay;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= StartPendingReplay;
                return;
            }

            LocalStart? localStart = UnityEngine.Object.FindAnyObjectByType<LocalStart>();
            if (localStart == null || !localStart.IsInitialized)
                return;

            EditorApplication.update -= StartPendingReplay;
            SessionState.EraseString(PendingReplayPathKey);
            try
            {
                localStart.LoadReplayFile(replayPath);
                localStart.SetReplayPlaying(true);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Replay", exception.Message, "OK");
            }
        }
    }
}
