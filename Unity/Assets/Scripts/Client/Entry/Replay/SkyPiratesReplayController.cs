using SlyPirates.Client.DI;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Services.Netcode;
using SlyPirates.Shared.Services.Replay;
using System;
using System.IO;
using UnityEngine;

namespace SlyPirates.Client.Entry.Replay
{
    [Inject]
    public sealed class SkyPiratesReplayController : MonoBehaviour
    {
        [Inject] private SkyPiratesSessionProvider _session = null!;
        [Inject] private IWorldHistory _history = null!;
        [Inject] private SkyPiratesTransientSimulation _simulation = null!;
        [Inject] private LocalGameLoop _gameLoop = null!;
        [Inject] private ITickableService<IPostTickable> _postTickables = null!;

        private SkyPiratesReplayRecorder? _recorder;
        private SkyPiratesReplayPlayback? _playback;
        private bool _replayMode;

        private void Update()
        {
            if (_session == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                ToggleReplayRecording();
            }

            if (!_replayMode)
            {
                return;
            }

            _playback?.Update(Time.unscaledDeltaTime);
            _postTickables.Tick(checked((int)_session.CurrentStep));
        }

        public void ToggleReplayRecording()
        {
            if (_recorder != null)
            {
                SaveReplayRecording();
                return;
            }

            if (!CanRecordReplay)
            {
                return;
            }

            long initialStep = _session.CurrentStep;
            WorldData initialWorld = _history.GetSnapshot(checked((int)initialStep));
            _recorder = new SkyPiratesReplayRecorder(_session.Session, _session.World, _history, initialWorld, initialStep);
            Delta.Diagnostics.Trace.Info($"[Replay] Recording started from saved step {initialStep}. Press F9 again to save.");
        }

        public void LoadReplayFile(string path)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The local game session has not started.");
            }

            _playback ??= new SkyPiratesReplayPlayback(_session, _simulation);
            _playback.Load(SkyPiratesReplayFile.Load(path, _session.World));
            _replayMode = true;
            _gameLoop.Enabled = false;
            _postTickables.Tick(checked((int)_playback.CurrentStep));
            Delta.Diagnostics.Trace.Info($"[Replay] Loaded {path}, frame={_playback.CurrentStep}..{_playback.LastStep}.");
        }

        public void SeekReplay(long step)
        {
            if (_playback == null)
            {
                return;
            }

            _playback.Seek(step);
            _postTickables.Tick(checked((int)step));
        }

        public void SetReplayPlaying(bool playing)
        {
            if (_playback != null)
            {
                _playback.Playing = playing;
            }
        }

        public bool IsInitialized => _session != null && _session.Session != null;

        public bool CanRecordReplay => IsInitialized && _session.IsReady && !_replayMode;

        public bool IsRecordingReplay => _recorder != null;

        public bool HasReplay => _playback != null;

        public bool IsReplayPlaying => _playback?.Playing ?? false;

        public SkyPiratesReplayPlayback? Replay => _playback;

        public long ReplayFrame => _playback?.CurrentStep ?? 0;

        public long ReplayFirstFrame => _playback?.FirstStep ?? 0;

        public long ReplayLastFrame => _playback?.LastStep ?? 0;

        public void StepReplay(int direction)
        {
            if (_playback == null)
            {
                return;
            }

            SeekReplay(Math.Max(_playback.FirstStep, Math.Min(_playback.LastStep, _playback.CurrentStep + direction)));
        }

        public bool VerifyReplayHash(long step, out ulong expected, out ulong actual)
        {
            if (_playback != null)
            {
                return _playback.VerifyHashAt(step, out expected, out actual);
            }

            expected = 0;
            actual = 0;
            return false;
        }

        private void SaveReplayRecording()
        {
            if (_recorder == null)
            {
                return;
            }

            try
            {
                string directory = GetReplayDirectory();
                Directory.CreateDirectory(directory);
                string fileName = $"skypirates-{DateTime.Now:yyyyMMdd-HHmmss-fff}";
                string path = Path.Combine(directory, $"{fileName}.json");
                int suffix = 1;
                while (File.Exists(path))
                {
                    path = Path.Combine(directory, $"{fileName}-{suffix++}.json");
                }

                SkyPiratesReplayFile.Save(path, _recorder.Capture(), _session.World);
                _recorder = null;
                Delta.Diagnostics.Trace.Info($"[Replay] Saved: {path}");
            }
            catch (Exception exception)
            {
                Delta.Diagnostics.Debug.Error(exception);
            }
        }

        private static string GetReplayDirectory()
        {
            var directory = new DirectoryInfo(Application.dataPath);
            while (directory != null)
            {
                bool hasGitMetadata = Directory.Exists(Path.Combine(directory.FullName, ".git"))
                    || File.Exists(Path.Combine(directory.FullName, ".git"));
                bool hasRootSolution = File.Exists(Path.Combine(directory.FullName, "SkyPirates.slnx"));
                if (hasGitMetadata && hasRootSolution)
                {
                    return Path.Combine(directory.FullName, "Replays");
                }

                directory = directory.Parent;
            }

            return Path.Combine(Application.persistentDataPath, "Replays");
        }
    }
}
