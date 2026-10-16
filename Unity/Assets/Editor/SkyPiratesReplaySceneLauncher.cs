using System;
using System.IO;
using SlyPirates.Client.Entry.Replay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlyPirates.Editor
{
    [InitializeOnLoad]
    internal static class SkyPiratesReplaySceneLauncher
    {
        private const string ReplayScenePath = "Assets/Scenes/ReplayScene.unity";
        private const string PendingReplayPathKey = "SlyPirates.ReplayScene.PendingReplayPath";
        private const string OriginalHistoryCapacityKey = "SlyPirates.ReplayScene.OriginalHistoryCapacity";

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

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            try
            {
                int historyCapacity = GetReplayHistoryCapacity(replayPath);
                Scene scene = EditorSceneManager.OpenScene(ReplayScenePath, OpenSceneMode.Single);
                if (!scene.IsValid())
                    throw new InvalidOperationException($"Could not open replay scene '{ReplayScenePath}'.");

                SlyPirates.Local.Entry.LocalStart localStart = FindLocalStart(scene)
                    ?? throw new InvalidOperationException($"Replay scene '{ReplayScenePath}' has no LocalStart component.");
                var serializedStart = new SerializedObject(localStart);
                SerializedProperty historyCapacityProperty = serializedStart.FindProperty("_historyCapacity")
                    ?? throw new InvalidOperationException("LocalStart has no _historyCapacity setting.");

                SessionState.SetInt(OriginalHistoryCapacityKey, historyCapacityProperty.intValue);
                historyCapacityProperty.intValue = historyCapacity;
                serializedStart.ApplyModifiedProperties();

                SessionState.SetString(PendingReplayPathKey, Path.GetFullPath(replayPath));
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                RestoreHistoryCapacity();
                ClearPendingReplay();
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
                RestoreHistoryCapacity();
                ClearPendingReplay();
            }
        }

        private static void LoadPendingReplay()
        {
            string replayPath = SessionState.GetString(PendingReplayPathKey, string.Empty);
            if (string.IsNullOrEmpty(replayPath) || !EditorApplication.isPlaying)
            {
                EditorApplication.update -= LoadPendingReplay;
                return;
            }

            SkyPiratesReplayController? replayController = UnityEngine.Object.FindAnyObjectByType<SkyPiratesReplayController>();
            if (replayController == null || !replayController.IsInitialized)
                return;

            EditorApplication.update -= LoadPendingReplay;
            SessionState.EraseString(PendingReplayPathKey);
            try
            {
                replayController.LoadReplayFile(replayPath);
                replayController.SetReplayPlaying(true);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Replay", exception.Message, "OK");
            }
        }

        private static int GetReplayHistoryCapacity(string replayPath)
        {
            ReplayHeader? replay = JsonUtility.FromJson<ReplayHeader>(File.ReadAllText(replayPath));
            if (replay?.EndHashCheck == null)
                throw new InvalidDataException("The selected file is not a valid Sky Pirates replay.");

            long frameCount = replay.EndHashCheck.Step - replay.InitialStep + 1;
            if (frameCount <= 0)
                throw new InvalidDataException("The replay has an invalid frame range.");

            return checked((int)frameCount);
        }

        private static SlyPirates.Local.Entry.LocalStart? FindLocalStart(Scene scene)
        {
            SlyPirates.Local.Entry.LocalStart? result = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (SlyPirates.Local.Entry.LocalStart localStart in root.GetComponentsInChildren<SlyPirates.Local.Entry.LocalStart>(true))
            {
                if (result != null)
                    throw new InvalidOperationException("Replay scene must contain exactly one LocalStart component.");
                result = localStart;
            }

            return result;
        }

        private static void RestoreHistoryCapacity()
        {
            int originalCapacity = SessionState.GetInt(OriginalHistoryCapacityKey, -1);
            if (originalCapacity < 0)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path == ReplayScenePath)
            {
                SlyPirates.Local.Entry.LocalStart? localStart = FindLocalStart(scene);
                if (localStart != null)
                {
                    var serializedStart = new SerializedObject(localStart);
                    SerializedProperty historyCapacityProperty = serializedStart.FindProperty("_historyCapacity");
                    if (historyCapacityProperty != null)
                    {
                        historyCapacityProperty.intValue = originalCapacity;
                        serializedStart.ApplyModifiedProperties();
                        if (scene.isDirty && !EditorSceneManager.SaveScene(scene, scene.path, false))
                            Debug.LogError($"Could not restore replay scene settings in '{scene.path}'.");
                    }
                }
            }

            SessionState.EraseInt(OriginalHistoryCapacityKey);
        }

        private static void ClearPendingReplay()
        {
            SessionState.EraseString(PendingReplayPathKey);
        }

        [Serializable]
        private sealed class ReplayHeader
        {
            public long InitialStep;
            public ReplayHashCheck? EndHashCheck;
        }

        [Serializable]
        private sealed class ReplayHashCheck
        {
            public long Step;
        }
    }
}
