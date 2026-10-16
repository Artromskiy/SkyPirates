using System;
using System.IO;
using SlyPirates.Client.Entry.Replay;
using SlyPirates.Shared.Services.Replay;
using UnityEditor;
using UnityEngine;

namespace SlyPirates.Editor
{
    public sealed class SkyPiratesReplayWindow : EditorWindow
    {
        private string _path = string.Empty;
        private SkyPiratesReplayController? _replayController;
        private long _frame;

        [MenuItem("Sky Pirates/Tools/Replay")]
        private static void Open() => GetWindow<SkyPiratesReplayWindow>("Sky Pirates Replay");

        private void OnEnable() => EditorApplication.update += Repaint;

        private void OnDisable() => EditorApplication.update -= Repaint;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Replay JSON", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _path = EditorGUILayout.TextField(_path);
                if (GUILayout.Button("Browse", GUILayout.Width(70)))
                {
                    string selected = EditorUtility.OpenFilePanel("Open Sky Pirates replay", "", "json");
                    if (!string.IsNullOrEmpty(selected))
                        _path = selected;
                }
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_path) || !File.Exists(_path) || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Open replay scene and start"))
                    SkyPiratesReplaySceneLauncher.OpenAndPlay(_path);
            }

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || string.IsNullOrEmpty(_path) || !File.Exists(_path)))
            {
                if (GUILayout.Button("Load replay into Play Mode"))
                {
                    _replayController = FindAnyObjectByType<SkyPiratesReplayController>();
                    if (_replayController == null)
                    {
                        EditorUtility.DisplayDialog("Replay", "No replay controller is active in the current scene.", "OK");
                        return;
                    }

                    _replayController.LoadReplayFile(_path);
                    _frame = _replayController.ReplayFrame;
                }
            }

            if (EditorApplication.isPlaying)
            {
                if (_replayController == null)
                    _replayController = FindAnyObjectByType<SkyPiratesReplayController>();
            }
            else
            {
                _replayController = null;
            }

            if (_replayController == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Recording", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!_replayController.IsInitialized ||
                       (!_replayController.IsRecordingReplay && !_replayController.CanRecordReplay)))
            {
                string buttonText = _replayController.IsRecordingReplay
                    ? "Stop and save recording (F9)"
                    : "Start recording (F9)";
                if (GUILayout.Button(buttonText))
                    _replayController.ToggleReplayRecording();
            }

            string recordingStatus = _replayController.IsRecordingReplay
                ? "Recording is active. Press F9 again or use the button above to save it."
                : _replayController.CanRecordReplay
                    ? "F9 starts and stops recording. Replays are saved to the repository's Replays folder when running from a checkout."
                    : "Recording is unavailable until the local session starts, and while replay playback is active.";
            EditorGUILayout.HelpBox(recordingStatus, MessageType.Info);

            if (!_replayController.HasReplay)
            {
                EditorGUILayout.HelpBox("Load a replay to enable playback controls.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            SkyPiratesReplayPlayback replay = _replayController.Replay!;
            replay.Mode = (ReplayRunMode)EditorGUILayout.EnumPopup("Playback mode", replay.Mode);
            if (replay.Mode == ReplayRunMode.RunFixedFps)
            {
                replay.FramesPerTick = Math.Max(1, EditorGUILayout.IntField("Transient frames / tick", replay.FramesPerTick));
                EditorGUILayout.HelpBox("Advances a fixed fraction of one tick per rendered frame.", MessageType.None);
            }
            else if (replay.Mode == ReplayRunMode.RunRealFps)
            {
                EditorGUILayout.HelpBox("Advances by elapsed real frame time, matching the live simulation's fractional steps.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Advances one fixed tick per rendered frame without transient microsteps.", MessageType.None);
            }

            replay.Speed = EditorGUILayout.Slider("Playback speed", (float)replay.Speed, 0.05f, 4f);
            replay.Loop = EditorGUILayout.Toggle("Loop", replay.Loop);

            long first = _replayController.ReplayFirstFrame;
            long last = _replayController.ReplayLastFrame;
            long current = _replayController.ReplayFrame;
            EditorGUILayout.LabelField($"Frame {current} / {last}");
            if (last > first)
            {
                double frameRange = (double)last - first;
                float progress = (float)(((double)current - first) / frameRange);
                EditorGUI.BeginChangeCheck();
                float selectedProgress = EditorGUILayout.Slider("Position", progress, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    long selectedFrame = first + (long)Math.Round(frameRange * selectedProgress);
                    _replayController.SeekReplay(selectedFrame);
                    _frame = selectedFrame;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("|<")) _replayController.SeekReplay(_replayController.ReplayFirstFrame);
                if (GUILayout.Button("<")) _replayController.StepReplay(-1);
                if (GUILayout.Button(_replayController.IsReplayPlaying ? "Pause" : "Play"))
                    _replayController.SetReplayPlaying(!_replayController.IsReplayPlaying);
                if (GUILayout.Button(">")) _replayController.StepReplay(1);
                if (GUILayout.Button(">|")) _replayController.SeekReplay(_replayController.ReplayLastFrame);
            }

            _frame = EditorGUILayout.LongField("Go to frame", _frame);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Seek"))
                    _replayController.SeekReplay(System.Math.Max(first, System.Math.Min(last, _frame)));
                if (GUILayout.Button("Verify hash here"))
                {
                    if (_replayController.VerifyReplayHash(_replayController.ReplayFrame, out ulong expected, out ulong actual))
                        EditorUtility.DisplayDialog("Replay hash", $"Match: 0x{actual:X16}", "OK");
                    else if (expected != 0)
                        EditorUtility.DisplayDialog("Replay hash", $"Mismatch\nExpected: 0x{expected:X16}\nActual: 0x{actual:X16}", "OK");
                    else
                        EditorUtility.DisplayDialog("Replay hash", "There is no hash check for this frame.", "OK");
                }
            }
        }
    }
}
