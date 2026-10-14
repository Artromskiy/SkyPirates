using Delta.Netcode;
using Delta.ECS.Unity;
using DVG.SkyPirates.Shared.Core;
using DVG.SkyPirates.Shared;
using DVG.SkyPirates.Client.DI;
using DVG.SkyPirates.Client.Entry;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Services.Netcode;
using DVG.SkyPirates.Shared.Services.Replay;
using DVG.SkyPirates.Client.Systems;
using SimpleInjector;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DVG.SkyPirates.Local.Entry
{
    public class LocalStart : MonoBehaviour
    {
        private Container _container = null!;
        private SkyPiratesSessionProvider _session = null!;
        private IHistorySystem _history = null!;
        private ViewBindingTickSystem _viewBinding = null!;
        private SkyPiratesReplayRecorder? _replayRecorder;
        private SkyPiratesReplayPlayback? _replayPlayback;
        private bool _replayMode;
        private bool _replayPlaying;
        private bool _spawnSquadSent;

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
                BeginReplayRecording();
            if (Input.GetKeyDown(KeyCode.F10))
                SaveReplayRecording();

            if (_replayMode)
            {
                if (_replayPlaying && _replayPlayback != null && _replayPlayback.CurrentStep < _replayPlayback.LastStep)
                    SeekReplay(_replayPlayback.CurrentStep + 1);
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

            _replayPlayback ??= new SkyPiratesReplayPlayback(_history, _session);
            _replayPlayback.Load(SkyPiratesReplayFile.Load(path));
            _replayMode = true;
            _replayPlaying = false;
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

        public void SetReplayPlaying(bool playing) => _replayPlaying = playing;

        public bool IsReplayPlaying => _replayPlaying;

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
