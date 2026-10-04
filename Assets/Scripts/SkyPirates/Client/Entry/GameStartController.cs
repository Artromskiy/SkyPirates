using Delta.Netcode;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Services.Netcode;
using System;
using System.Diagnostics;

namespace DVG.SkyPirates.Client.Entry
{
    public sealed class GameStartController
    {
        private readonly SkyPiratesSessionProvider _session;
        private readonly IClientService _client;
        private readonly ITickableService<IPreTickable> _preTickableService;
        private readonly ITickableService<IPostTickable> _postTickableService;
        private readonly Stopwatch _clock = new();
        private long _startStep;
        private long _targetStep;

        public GameStartController(
            SkyPiratesSessionProvider session,
            IClientService client,
            ITickableService<IPreTickable> preTickableService,
            ITickableService<IPostTickable> postTickableService,
            ICommandReciever commandReceiver)
        {
            _session = session;
            _client = client;
            _preTickableService = preTickableService;
            _postTickableService = postTickableService;
            commandReceiver.RegisterReciever<TickSyncCommand>(OnSyncTick);
            _session.Ready += ResetClock;
            if (_session.IsReady)
                ResetClock();
        }

        public void Update()
        {
            if (!_session.IsReady)
            {
                _client.Tick(0);
                return;
            }

            if (!_clock.IsRunning)
                _clock.Start();

            long elapsedSteps = _clock.Elapsed.Ticks * Constants.TicksPerSecond / TimeSpan.TicksPerSecond;
            long targetStep = _startStep + elapsedSteps;
            if (_session.CurrentStep == targetStep)
                return;

            int tick = checked((int)targetStep);
            _preTickableService.Tick(tick);
            _session.Tick(targetStep);
            _postTickableService.Tick(tick);
            _targetStep = targetStep;
        }

        private void OnSyncTick(Command<TickSyncCommand> command)
        {
            long serverStep = command.Header.Step;
            if (serverStep <= _targetStep)
                return;

            _startStep = serverStep;
            _targetStep = serverStep;
            _clock.Restart();
        }

        private void ResetClock()
        {
            _startStep = System.Math.Max(0, _session.CurrentStep);
            _targetStep = _session.CurrentStep;
            _clock.Restart();
        }
    }
}
