using System;
using System.Buffers;
using System.Diagnostics;
using Delta.Netcode;
using SkyPirates.Shared;
using SkyPirates.Shared.Commands;
using SkyPirates.Shared.IServices;

namespace SkyPirates.Shared.Services.Replay
{
    public enum SkyPiratesReplayRunMode
    {
        RunRealFps,
        RunFixedFps,
        Fast,
    }

    public readonly struct SkyPiratesReplayRunStatistics
    {
        public long Ticks { get; }
        public long TransientFrames { get; }
        public int MinimumFramesPerTick { get; }
        public int MaximumFramesPerTick { get; }

        public double AverageFramesPerTick => Ticks == 0 ? 0 : TransientFrames / (double)Ticks;

        internal SkyPiratesReplayRunStatistics(long ticks, long transientFrames, int minimumFramesPerTick, int maximumFramesPerTick)
        {
            Ticks = ticks;
            TransientFrames = transientFrames;
            MinimumFramesPerTick = minimumFramesPerTick;
            MaximumFramesPerTick = maximumFramesPerTick;
        }
    }

    public sealed class SkyPiratesReplayFrameRunner
    {
        private readonly SkyPiratesReplayPlayback _playback;
        private readonly ITransientSimulation<JoystickCommand> _transientSimulation;
        private readonly ArrayBufferWriter<byte> _fixedStepState = new();
        private long _ticks;
        private long _transientFrames;
        private int _minimumFramesPerTick;
        private int _maximumFramesPerTick;

        public bool Loop { get; set; }

        public long CurrentStep => _playback.CurrentStep;

        public long LastStep => _playback.LastStep;

        public SkyPiratesReplayRunStatistics Statistics => new(
            _ticks,
            _transientFrames,
            _minimumFramesPerTick,
            _maximumFramesPerTick);

        public SkyPiratesReplayFrameRunner(
            SkyPiratesReplayPlayback playback,
            ITransientSimulation<JoystickCommand> transientSimulation)
        {
            _playback = playback ?? throw new ArgumentNullException(nameof(playback));
            _transientSimulation = transientSimulation ?? throw new ArgumentNullException(nameof(transientSimulation));
            CaptureFixedStepState();
        }

        public void Seek(long step)
        {
            RestoreFixedStepState();
            _playback.Seek(step, _transientSimulation);
            CaptureFixedStepState();
        }

        public void ResetStatistics()
        {
            _ticks = 0;
            _transientFrames = 0;
            _minimumFramesPerTick = 0;
            _maximumFramesPerTick = 0;
        }

        public bool TryAdvanceOneTick(SkyPiratesReplayRunMode mode, int framesPerTick, out int transientFrames)
        {
            transientFrames = 0;
            if (mode == SkyPiratesReplayRunMode.RunFixedFps && framesPerTick <= 0)
                throw new ArgumentOutOfRangeException(nameof(framesPerTick), "Frames per tick must be greater than zero.");
            if (mode != SkyPiratesReplayRunMode.Fast
                && mode != SkyPiratesReplayRunMode.RunFixedFps
                && mode != SkyPiratesReplayRunMode.RunRealFps)
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown replay run mode.");

            if (_playback.CurrentStep >= _playback.LastStep)
            {
                if (!Loop || _playback.FirstStep >= _playback.LastStep)
                    return false;

                Seek(_playback.FirstStep);
                return true;
            }

            // Transient frames are predicted from the last fixed state. Discard any transient
            // changes left by the previous rendered replay step before applying the next one.
            RestoreFixedStepState();
            _playback.Seek(_playback.CurrentStep + 1, _transientSimulation);
            CaptureFixedStepState();

            JoystickCommand noInput = default;
            switch (mode)
            {
                case SkyPiratesReplayRunMode.Fast:
                    break;
                case SkyPiratesReplayRunMode.RunFixedFps:
                    transientFrames = RunFixedFrames(in noInput, framesPerTick);
                    break;
                case SkyPiratesReplayRunMode.RunRealFps:
                    transientFrames = RunRealTimeFrames(in noInput);
                    break;
            }

            RecordTick(transientFrames);
            return true;
        }

        private int RunFixedFrames(in JoystickCommand input, int framesPerTick)
        {
            double deltaTime = 1d / Constants.TicksPerSecond / framesPerTick;
            for (int frame = 0; frame < framesPerTick; frame++)
                _transientSimulation.TickTransient(in input, deltaTime);

            return framesPerTick;
        }

        private int RunRealTimeFrames(in JoystickCommand input)
        {
            long tickStart = Stopwatch.GetTimestamp();
            long previousFrame = tickStart;
            long tickBudget = Stopwatch.Frequency / Constants.TicksPerSecond;
            int frames = 0;

            do
            {
                long frameStart = Stopwatch.GetTimestamp();
                double deltaTime = (frameStart - previousFrame) / (double)Stopwatch.Frequency;
                _transientSimulation.TickTransient(in input, deltaTime);
                previousFrame = frameStart;
                frames++;
            }
            while (Stopwatch.GetTimestamp() - tickStart < tickBudget);

            return frames;
        }

        private void CaptureFixedStepState()
        {
            _fixedStepState.Clear();
            _transientSimulation.Save(_fixedStepState);
        }

        private void RestoreFixedStepState()
        {
            _transientSimulation.Load(_fixedStepState.WrittenSpan);
        }

        private void RecordTick(int transientFrames)
        {
            _ticks++;
            _transientFrames += transientFrames;
            if (_ticks == 1)
            {
                _minimumFramesPerTick = transientFrames;
                _maximumFramesPerTick = transientFrames;
                return;
            }

            _minimumFramesPerTick = Math.Min(_minimumFramesPerTick, transientFrames);
            _maximumFramesPerTick = Math.Max(_maximumFramesPerTick, transientFrames);
        }
    }
}
