using Delta.Netcode;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.Services.Netcode;
using System;
using System.Buffers;
using System.Diagnostics;

namespace DVG.SkyPirates.Client.Entry
{
    public sealed class GameStartController
    {
        private readonly SkyPiratesSessionProvider _session;
        private readonly IClientService _client;
        private readonly ITransientSimulation<JoystickCommand> _simulation;
        private readonly ICommandSendScheduler _commandSendScheduler;
        private readonly Stopwatch _clock = new();
        private ArrayBufferWriter<byte> _checkpoint = new();
        private long _startStep;
        private long _targetStep;
        private double _lastTransientElapsedSeconds;

        public GameStartController(
            SkyPiratesSessionProvider session,
            IClientService client,
            ITransientSimulation<JoystickCommand> simulation,
            ICommandSendScheduler commandSendScheduler,
            ICommandReciever commandReceiver)
        {
            _session = session;
            _client = client;
            _simulation = simulation;
            _commandSendScheduler = commandSendScheduler;
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
            double elapsedSeconds = _clock.Elapsed.TotalSeconds;
            if (_session.CurrentStep != targetStep)
            {
                if (_checkpoint.WrittenCount == 0)
                    CaptureCheckpoint();

                _simulation.Load(_checkpoint.WrittenSpan);
                _simulation.Tick(targetStep);
                _targetStep = targetStep;
                CaptureCheckpoint();
                _lastTransientElapsedSeconds = elapsedSteps / (double)Constants.TicksPerSecond;
            }

            double transientDelta = elapsedSeconds - _lastTransientElapsedSeconds;
            _lastTransientElapsedSeconds = elapsedSeconds;
            _commandSendScheduler.TryGetTransientInput(out JoystickCommand input);
            _simulation.TickTransient(in input, transientDelta);
        }

        private void OnSyncTick(Command<TickSyncCommand> command)
        {
            long serverStep = command.Header.Step;
            if (serverStep <= _targetStep)
                return;

            _startStep = serverStep;
            _targetStep = serverStep;
            _clock.Restart();
            _lastTransientElapsedSeconds = 0;
            CaptureCheckpoint();
        }

        private void ResetClock()
        {
            _startStep = System.Math.Max(0, _session.CurrentStep);
            _targetStep = _session.CurrentStep;
            _clock.Restart();
            _lastTransientElapsedSeconds = 0;
            CaptureCheckpoint();
        }

        private void CaptureCheckpoint()
        {
            _checkpoint = new ArrayBufferWriter<byte>();
            _simulation.Save(_checkpoint);
        }
    }
}
