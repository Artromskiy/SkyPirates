using System;
using System.IO;
using DVG.SkyPirates.Local.Entry;
using UnityEditor;
using UnityEngine;

namespace DVG.SkyPirates.Editor
{
    public sealed class SkyPiratesReplayWindow : EditorWindow
    {
        private string _path = string.Empty;
        private LocalStart? _localStart;
        private long _frame;
        private bool _frameInitialized;

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

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || string.IsNullOrEmpty(_path)))
            {
                if (GUILayout.Button("Load replay into Play Mode"))
                {
                    _localStart = FindAnyObjectByType<LocalStart>();
                    if (_localStart == null)
                    {
                        EditorUtility.DisplayDialog("Replay", "No LocalStart component is active in the current scene.", "OK");
                        return;
                    }

                    _localStart.LoadReplayFile(_path);
                    _frame = _localStart.ReplayFrame;
                    _frameInitialized = true;
                }
            }

            if (_localStart == null && EditorApplication.isPlaying)
            {
                _localStart = FindAnyObjectByType<LocalStart>();
                _frameInitialized = false;
            }

            if (_localStart == null)
                return;

            if (!_localStart.HasReplay)
            {
                EditorGUILayout.HelpBox("Load a replay to enable playback controls.", MessageType.Info);
                return;
            }

            if (!_frameInitialized)
            {
                _frame = _localStart.ReplayFrame;
                _frameInitialized = true;
            }

            EditorGUILayout.Space();
            long first = _localStart.ReplayFirstFrame;
            long last = _localStart.ReplayLastFrame;
            long current = _localStart.ReplayFrame;
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
                    _localStart.SeekReplay(selectedFrame);
                    _frame = selectedFrame;
                    _frameInitialized = true;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("|<")) _localStart.SeekReplay(_localStart.ReplayFirstFrame);
                if (GUILayout.Button("<")) _localStart.StepReplay(-1);
                if (GUILayout.Button(_localStart.IsReplayPlaying ? "Pause" : "Play"))
                    _localStart.SetReplayPlaying(!_localStart.IsReplayPlaying);
                if (GUILayout.Button(">")) _localStart.StepReplay(1);
                if (GUILayout.Button(">|")) _localStart.SeekReplay(_localStart.ReplayLastFrame);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                float speed = EditorGUILayout.Slider("Playback speed", _localStart.ReplaySpeed, 0.1f, 1f);
                EditorGUILayout.LabelField($"{speed:0.00}×", GUILayout.Width(44));
                _localStart.SetReplaySpeed(speed);
            }

            _frame = EditorGUILayout.LongField("Go to frame", _frame);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Seek"))
                    _localStart.SeekReplay(System.Math.Max(first, System.Math.Min(last, _frame)));
                if (GUILayout.Button("Verify hash here"))
                {
                    if (_localStart.VerifyReplayHash(_localStart.ReplayFrame, out ulong expected, out ulong actual))
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
