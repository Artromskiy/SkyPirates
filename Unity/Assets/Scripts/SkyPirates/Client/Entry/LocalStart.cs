using Delta.Netcode;
using Delta.ECS.Unity;
using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Client.DI;
using SkyPirates.Client.Entry;
using SkyPirates.Client.IServices;
using SkyPirates.Shared.Commands;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.IServices;
using SkyPirates.Shared.IServices.TickableExecutors;
using SkyPirates.Shared.Services.Netcode;
using SkyPirates.Shared.Services.Replay;
using SkyPirates.Client.Systems;
using SimpleInjector;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SkyPirates.Local.Entry
{
    public class LocalStart : MonoBehaviour
    {
        private Container _container = null!;
        private SkyPiratesSessionProvider _session = null!;
        private IHistorySystem _history = null!;
        private ViewBindingTickSystem _viewBinding = null!;
        private SkyPiratesReplayRecorder? _replayRecorder;
        private SkyPiratesReplayPlayback? _replayPlayback;
        private SkyPiratesReplayFrameRunner? _replayFrameRunner;
        private SkyPiratesReplayRunMode _replayRunMode;
        private int _replayFramesPerTick = 1;
        private bool _replayMode;
        private bool _replayPlaying;
        private bool _spawnSquadSent;
        private float _replayStepAccumulator;
        private float _replaySpeed = 1f;

        public bool IsInitialized => _container != null && _session != null;

        private void Start()
        {
            try
            {
                Delta.Diagnostics.Trace.Info("[LocalStart] Container creation");
                var sceneWorld = SceneWorld.GetForScene(gameObject.scene);
                _container = new LocalContainer(sceneWorld);
                Delta.Diagnostics.Trace.Info("[LocalStart] Container register and inject ViewModels");
                _container.RegisterAndInjectViewModels();
                _viewBinding = _container.GetAllInstances<IPostTickable>().OfType<ViewBindingTickSystem>().Single();

                Delta.Diagnostics.Trace.Info("[LocalStart] Container get instances");
                var client = _container.GetInstance<IClientService>();
                var worldData = _container.GetInstance<IPathFactory<WorldData>>().Create("Configs/Maps/Map1");
                Delta.Diagnostics.Trace.Info("[LocalStart] Load map");
                _history = _container.GetInstance<IHistorySystem>();
                _history.ApplySnapshot(worldData);
                _viewBinding.Tick(0);
                _history.SaveBaseline();
                _session = _container.GetInstance<SkyPiratesSessionProvider>();
                _session.Start(new Delta.Netcode.AuthorId((uint)client.Id));
            }
            catch (Exception e)
            {
                Delta.Diagnostics.Debug.Error(e);
            }
        }

        private void Update()
        {
            if (_container == null || _session == null)
                return;

            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (_replayRecorder == null)
                    BeginReplayRecording();
                else
                    SaveReplayRecording();
            }

            if (_replayMode)
            {
                if (_replayPlaying && _replayPlayback != null && _replayPlayback.CurrentStep < _replayPlayback.LastStep)
                {
                    _replayStepAccumulator += _replaySpeed;
                    int stepsToAdvance = (int)_replayStepAccumulator;
                    if (stepsToAdvance > 0)
                    {
                        _replayStepAccumulator -= stepsToAdvance;
                        for (int i = 0; i < stepsToAdvance; i++)
                        {
                            if (_replayFrameRunner == null
                                || !_replayFrameRunner.TryAdvanceOneTick(_replayRunMode, _replayFramesPerTick, out _))
                            {
                                break;
                            }

                            _viewBinding.Tick(checked((int)_replayFrameRunner.CurrentStep));
                        }

                        if (_replayPlayback.CurrentStep >= _replayPlayback.LastStep)
                            SetReplayPlaying(false);
                    }
                }
                else if (_replayPlaying)
                {
                    SetReplayPlaying(false);
                }

                return;
            }

            var startController = _container.GetInstance<GameStartController>();
            startController.Update();

            if (!_spawnSquadSent && _session.CurrentStep >= 0)
            {
                Delta.Diagnostics.Trace.Info("[LocalStart] Spawn squad");
                _session.Send(new SpawnSquadCommand(), _session.CurrentStep + 1);
                _spawnSquadSent = true;
            }
        }

        private void BeginReplayRecording()
        {
            if (_session.Session == null || !_session.IsReady)
                return;

            long initialStep = _session.CurrentStep;
            int snapshotStep = initialStep < 0 ? int.MinValue : checked((int)initialStep);
            WorldData initialWorld = _history.GetSnapshot(snapshotStep);
            _replayRecorder = new SkyPiratesReplayRecorder(
                _session.Session,
                _history,
                initialWorld,
                initialStep);
            Delta.Diagnostics.Trace.Info($"[LocalStart] Replay recording started from saved step {initialStep}. Press F9 again to save.");
        }

        private void SaveReplayRecording()
        {
            if (_replayRecorder == null)
                return;

            try
            {
                string directory = GetReplayDirectory();
                Directory.CreateDirectory(directory);
                string fileName = $"skypirates-{DateTime.Now:yyyyMMdd-HHmmss-fff}.json";
                string path = Path.Combine(directory, fileName);
                int suffix = 1;
                while (File.Exists(path))
                    path = Path.Combine(directory, $"skypirates-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{suffix++}.json");

                SkyPiratesReplayFile.Save(path, _replayRecorder.Capture());
                _replayRecorder = null;
                Delta.Diagnostics.Trace.Info($"[LocalStart] Replay saved: {path}");
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
                    return Path.Combine(directory.FullName, "Replays");

                directory = directory.Parent;
            }

            return Path.Combine(Application.persistentDataPath, "Replays");
        }

        public void LoadReplayFile(string path)
        {
            if (_container == null)
                throw new InvalidOperationException("The local game container has not started.");

            _replayPlayback ??= new SkyPiratesReplayPlayback(_history, _session);
            _replayPlayback.Load(SkyPiratesReplayFile.Load(path));
            _replayFrameRunner = new SkyPiratesReplayFrameRunner(
                _replayPlayback,
                _container.GetInstance<ITransientSimulation<JoystickCommand>>());
            _replayMode = true;
            _replayPlaying = false;
            _replayStepAccumulator = 0f;
            _replaySpeed = 1f;
            _replayFrameRunner.ResetStatistics();
            _viewBinding.Tick(checked((int)_replayPlayback.CurrentStep));
            Delta.Diagnostics.Trace.Info($"[LocalStart] Replay loaded from {path}, frame={_replayPlayback.CurrentStep}..{_replayPlayback.LastStep}.");
        }

        public void SeekReplay(long step)
        {
            if (_replayPlayback == null)
                return;

            SetReplayPlaying(false);
            _replayFrameRunner?.ResetStatistics();
            SeekReplayInternal(step);
        }

        private void SeekReplayInternal(long step)
        {
            _replayPlayback!.Seek(step);
            _viewBinding.Tick(checked((int)step));
        }

        public void SetReplayPlaying(bool playing)
        {
            _replayStepAccumulator = 0f;
            _replayPlaying = playing
                && _replayPlayback != null
                && _replayPlayback.CurrentStep < _replayPlayback.LastStep;
        }

        public void SetReplaySpeed(float speed)
        {
            speed = Mathf.Clamp(speed, 0.1f, 1f);
            if (Mathf.Approximately(_replaySpeed, speed))
                return;

            _replaySpeed = speed;
            _replayStepAccumulator = 0f;
        }

        public void SetReplayRunMode(SkyPiratesReplayRunMode mode)
        {
            if (_replayRunMode == mode)
                return;

            _replayRunMode = mode;
            _replayFrameRunner?.ResetStatistics();
        }

        public void SetReplayFramesPerTick(int framesPerTick)
        {
            framesPerTick = System.Math.Max(1, framesPerTick);
            if (_replayFramesPerTick == framesPerTick)
                return;

            _replayFramesPerTick = framesPerTick;
            _replayFrameRunner?.ResetStatistics();
        }

        public bool HasReplay => _replayPlayback != null;

        public bool IsReplayPlaying => _replayPlaying;

        public float ReplaySpeed => _replaySpeed;

        public SkyPiratesReplayRunMode ReplayRunMode => _replayRunMode;

        public int ReplayFramesPerTick => _replayFramesPerTick;

        public SkyPiratesReplayRunStatistics ReplayRunStatistics => _replayFrameRunner?.Statistics ?? default;

        public long ReplayFrame => _replayPlayback?.CurrentStep ?? 0;

        public long ReplayFirstFrame => _replayPlayback?.FirstStep ?? 0;

        public long ReplayLastFrame => _replayPlayback?.LastStep ?? 0;

        public void StepReplay(int direction)
        {
            if (_replayPlayback == null)
                return;

            SeekReplay(System.Math.Max(_replayPlayback.FirstStep, System.Math.Min(_replayPlayback.LastStep, _replayPlayback.CurrentStep + direction)));
        }

        public bool VerifyReplayHash(long step, out ulong expected, out ulong actual)
        {
            if (_replayPlayback != null)
                return _replayPlayback.VerifyHashAt(step, out expected, out actual);

            expected = 0;
            actual = 0;
            return false;
        }
    }
}
