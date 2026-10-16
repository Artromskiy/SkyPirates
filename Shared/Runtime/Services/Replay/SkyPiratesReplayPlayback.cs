using Delta.Netcode;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Services.Netcode;
using SlyPirates.Shared.Tools.Json;
using System;

namespace SlyPirates.Shared.Services.Replay
{
    public enum ReplayRunMode { RunRealFps, RunFixedFps, Fast }

    /// <summary>Playback clock and immutable command input for the ordinary session model.</summary>
    public sealed class SkyPiratesReplayPlayback
    {
        private readonly SkyPiratesSessionProvider _session;
        private readonly SkyPiratesTransientSimulation _simulation;
        private SkyPiratesReplayData? _replay;
        private double _position;
        private double _speed = 1;
        private int _framesPerTick = 1;

        public SkyPiratesReplayPlayback(SkyPiratesSessionProvider session,
            SkyPiratesTransientSimulation simulation)
        { _session = session; _simulation = simulation; }

        public long CurrentStep => _session.CurrentStep;
        public long FirstStep => _replay?.InitialStep ?? 0;
        public long LastStep => _replay?.EndHashCheck.Step ?? 0;
        public bool Playing { get; set; }
        public bool Loop { get; set; }
        public ReplayRunMode Mode { get; set; } = ReplayRunMode.RunRealFps;
        public double Speed
        {
            get => _speed;
            set
            {
                if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
                _speed = value;
            }
        }
        public int FramesPerTick
        {
            get => _framesPerTick;
            set { if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); _framesPerTick = value; }
        }
        public int CompletedLoops { get; private set; }

        public void Load(SkyPiratesReplayData replay)
        {
            if (replay == null) throw new ArgumentNullException(nameof(replay));
            if (replay.EndHashCheck.Step < replay.InitialStep) throw new ArgumentException("Replay ends before its initial state.", nameof(replay));
            foreach (var command in replay.Commands)
                if (command.Step <= replay.InitialStep || command.Step > replay.EndHashCheck.Step)
                    throw new ArgumentException("Replay commands must be inside its simulation range.", nameof(replay));
            _session.ApplySnapshot(replay.InitialWorld, checked((int)replay.InitialStep));
            var start = new SessionStart(new SessionId(1), new AuthorId(0), new ProtocolId(1), checked(replay.InitialStep + 1), 0);
            _session.Restart(start);
            foreach (var command in replay.Commands)
            {
                var key = new CommandKey(start.SessionId, new AuthorId(command.AuthorId), command.Sequence);
                var header = new CommandHeader(key, command.TypeId, command.Step, command.Order);
                _session.ApplyReplayCommand(header, command.Payload);
            }
            _session.LockReplayCommands();
            _replay = replay;
            _position = 0;
            Playing = false;
            CompletedLoops = 0;
            _simulation.Tick(replay.InitialStep);
        }

        public void Seek(long targetStep)
        {
            EnsureLoaded();
            if (targetStep < FirstStep || targetStep > LastStep) throw new ArgumentOutOfRangeException(nameof(targetStep));
            _simulation.Tick(targetStep);
            _position = targetStep - FirstStep;
        }

        // Unity supplies unscaled frame time. A headless benchmark supplies measured elapsed
        // frame time. Both call the same transient simulation used by GameStartController.
        public void Update(double elapsedSeconds)
        {
            EnsureLoaded();
            if (elapsedSeconds < 0 || double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds))
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (!Playing) return;
            double delta = Mode == ReplayRunMode.RunRealFps ? elapsedSeconds * Constants.TicksPerSecond
                : Mode == ReplayRunMode.RunFixedFps ? 1d / FramesPerTick : 1d;
            _position = Math.Min(LastStep - FirstStep, _position + delta * Speed);
            if (_position + 1e-10 >= LastStep - FirstStep) _position = LastStep - FirstStep;
            long fixedStep = FirstStep + (long)Math.Floor(_position + 1e-10);
            double fraction = Math.Max(0, _position - (fixedStep - FirstStep));
            var input = default(JoystickCommand);
            _simulation.AdvanceFrame(fixedStep, fraction / Constants.TicksPerSecond, in input);
            if (_position >= LastStep - FirstStep)
            {
                if (Loop) { CompletedLoops++; Seek(FirstStep); }
                else Playing = false;
            }
        }

        public bool VerifyHashAt(long step, out ulong expected, out ulong actual)
        {
            EnsureLoaded();
            if (_replay!.EndHashCheck.Step != step) { expected = actual = 0; return false; }
            expected = _replay.EndHashCheck.Hash;
            WorldData snapshot = _session.GetSnapshot(checked((int)step));
            actual = SerializationUTF8.GetHashSum(snapshot);
            return actual == expected;
        }

        private void EnsureLoaded()
        {
            if (_replay == null) throw new InvalidOperationException("Load a replay before playback.");
        }
    }
}
