using Delta.Netcode;
using Delta.ECS.Unity;
using SlyPirates.Shared.Core;
using SlyPirates.Shared;
using SlyPirates.Client.DI;
using SlyPirates.Client.Entry;
using SlyPirates.Client.IServices;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Services.Netcode;
using SlyPirates.Shared.Services.Replay;
using SlyPirates.Client.Systems;
using SimpleInjector;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SlyPirates.Local.Entry
{
    public class LocalStart : MonoBehaviour
    {
        [SerializeField, Min(1)]
        [Tooltip("Number of retained fixed states. For a replay, include every tick and its initial state.")]
        private int _historyCapacity = Constants.MaxHistoryTicks;

        private Container _container = null!;
        private SkyPiratesSessionProvider _session = null!;
        private ViewBindingTickSystem _viewBinding = null!;
        private SkyPiratesReplayRecorder? _replayRecorder;
        private SkyPiratesReplayPlayback? _replayPlayback;
        private bool _replayMode;
        private bool _spawnSquadSent;

        private void Start()
        {
            try
            {
                Delta.Diagnostics.Trace.Info("[LocalStart] Container creation");
                var sceneWorld = SceneWorld.GetForScene(gameObject.scene);
                _container = new LocalContainer(sceneWorld, _historyCapacity);
                Delta.Diagnostics.Trace.Info("[LocalStart] Container register and inject ViewModels");
                _container.RegisterAndInjectViewModels();
                _viewBinding = _container.GetAllInstances<IPostTickable>().OfType<ViewBindingTickSystem>().Single();

                Delta.Diagnostics.Trace.Info("[LocalStart] Container get instances");
                var client = _container.GetInstance<IClientService>();
                var worldData = _container.GetInstance<IPathFactory<WorldData>>().Create("Configs/Maps/Map1");
                Delta.Diagnostics.Trace.Info("[LocalStart] Load map");
                _session = _container.GetInstance<SkyPiratesSessionProvider>();
                _session.ApplySnapshot(worldData, -1);
                _viewBinding.Tick(0);
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
                BeginReplayRecording();
            if (Input.GetKeyDown(KeyCode.F10))
                SaveReplayRecording();

            if (_replayMode)
            {
                _replayPlayback?.Update(Time.unscaledDeltaTime);
                _viewBinding.Tick(checked((int)_session.CurrentStep));
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
            int snapshotStep = checked((int)initialStep);
            var history = _container.GetInstance<IWorldHistory>();
            WorldData initialWorld = history.GetSnapshot(snapshotStep);
            _replayRecorder = new SkyPiratesReplayRecorder(
                _session.Session,
                history,
                initialWorld,
                initialStep);
            Delta.Diagnostics.Trace.Info($"[LocalStart] Replay recording started from saved step {initialStep}. Press F10 to save.");
        }

        private void SaveReplayRecording()
        {
            if (_replayRecorder == null)
                return;

            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "Replays");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, $"skypirates-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                SkyPiratesReplayFile.Save(path, _replayRecorder.Capture());
                _replayRecorder = null;
                Delta.Diagnostics.Trace.Info($"[LocalStart] Replay saved: {path}");
            }
            catch (Exception exception)
            {
                Delta.Diagnostics.Debug.Error(exception);
            }
        }

        public void LoadReplayFile(string path)
        {
            if (_container == null)
                throw new InvalidOperationException("The local game container has not started.");

            _replayPlayback ??= new SkyPiratesReplayPlayback(_session, _container.GetInstance<SkyPiratesTransientSimulation>());
            _replayPlayback.Load(SkyPiratesReplayFile.Load(path));
            _replayMode = true;
            _viewBinding.Tick(checked((int)_replayPlayback.CurrentStep));
            Delta.Diagnostics.Trace.Info($"[LocalStart] Replay loaded from {path}, frame={_replayPlayback.CurrentStep}..{_replayPlayback.LastStep}.");
        }

        public void SeekReplay(long step)
        {
            if (_replayPlayback == null)
                return;

            _replayPlayback.Seek(step);
            _viewBinding.Tick(checked((int)step));
        }

        public void SetReplayPlaying(bool playing) { if (_replayPlayback != null) _replayPlayback.Playing = playing; }

        public bool IsReplayPlaying => _replayPlayback?.Playing ?? false;

        public SkyPiratesReplayPlayback? Replay => _replayPlayback;

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
