using SlyPirates.Client.IServices;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Services.Netcode;
using SlyPirates.Shared;
using System;
using System.Diagnostics;

namespace SlyPirates.Client.Entry
{
    public sealed class GameStartController
    {
        private readonly SkyPiratesSessionProvider _session;
        private readonly IClientService _client;
        private readonly SkyPiratesTransientSimulation _simulation;
        private readonly ICommandSendScheduler _commandSendScheduler;
        private readonly Stopwatch _clock = new();
        private long _startStep;
        private long _targetStep;

        public GameStartController(
            SkyPiratesSessionProvider session,
            IClientService client,
            SkyPiratesTransientSimulation simulation,
            ICommandSendScheduler commandSendScheduler)
        {
            _session = session;
            _client = client;
            _simulation = simulation;
            _commandSendScheduler = commandSendScheduler;
            _session.ServerStepReceived += OnSyncTick;
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

            TimeSpan elapsed = _clock.Elapsed;
            long elapsedSteps = elapsed.Ticks * Constants.TicksPerSecond / TimeSpan.TicksPerSecond;
            long targetStep = _startStep + elapsedSteps;
            double elapsedSeconds = elapsed.TotalSeconds;
            _targetStep = targetStep;
            _commandSendScheduler.TryGetTransientInput(out JoystickCommand input);
            double fraction = elapsedSeconds - elapsedSteps / (double)Constants.TicksPerSecond;
            _simulation.AdvanceFrame(targetStep, fraction, in input);
        }

        private void OnSyncTick(long serverStep)
        {
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
