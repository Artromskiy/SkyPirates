using Delta.Netcode;
using SkyPirates.Shared.Commands;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.IServices.TickableExecutors;
using SkyPirates.Shared.Services.Netcode;
using System;
using System.Collections.Generic;

namespace SkyPirates.Shared.Services.Replay
{
    public sealed class SkyPiratesReplayPlayback
    {
        private const int CheckpointInterval = 32;
        private const int MaximumCachedCheckpoints = 8;

        private readonly IHistorySystem _history;
        private readonly SkyPiratesSessionProvider _session;
        private readonly SortedDictionary<long, SessionSnapshot> _checkpoints = new();
        private SkyPiratesReplayData? _replay;

        public SkyPiratesReplayPlayback(IHistorySystem history, SkyPiratesSessionProvider session)
        {
            _history = history;
            _session = session;
        }

        public long CurrentStep => _session.CurrentStep;

        public long FirstStep => _replay?.InitialStep ?? 0;

        public long LastStep
        {
            get
            {
                if (_replay == null)
                    return 0;

                return _replay.EndHashCheck.Step;
            }
        }

        public void Load(SkyPiratesReplayData replay)
        {
            _replay = replay ?? throw new ArgumentNullException(nameof(replay));
            _checkpoints.Clear();
            ResetSession();
        }

        public void Seek(long targetStep)
            => SeekCore(targetStep, null);

        internal void Seek(long targetStep, ITransientSimulation<JoystickCommand> simulation)
            => SeekCore(targetStep, simulation);

        private void SeekCore(long targetStep, ITransientSimulation<JoystickCommand>? simulation)
        {
            if (_replay == null)
                throw new InvalidOperationException("Load a replay before seeking.");
            if (targetStep < _replay.InitialStep || targetStep > LastStep)
                throw new ArgumentOutOfRangeException(nameof(targetStep));

            if (targetStep < _session.CurrentStep)
                RestoreNearestCheckpoint(targetStep);

            while (_session.CurrentStep < targetStep)
            {
                AdvanceOneStep(simulation);
            }
        }

        private void AdvanceOneStep(ITransientSimulation<JoystickCommand>? simulation)
        {
            if (_session.CurrentStep >= LastStep)
                throw new InvalidOperationException("The replay is already at its last step.");

            long nextStep = _session.CurrentStep + 1;
            if (simulation == null)
                _session.Session.Tick(nextStep);
            else
                simulation.Tick(nextStep);

            if (_session.CurrentStep != nextStep)
                throw new InvalidOperationException($"The replay simulation did not advance to step {nextStep}.");

            if ((nextStep - _replay.InitialStep) % CheckpointInterval == 0)
                CacheCheckpoint(_session.Session.CaptureSnapshot());
        }

        public bool VerifyHashAt(long step, out ulong expected, out ulong actual)
        {
            if (_replay == null)
                throw new InvalidOperationException("Load a replay before verifying it.");

            SkyPiratesReplayHashCheck check = _replay.EndHashCheck;
            if (check.Step != step)
            {
                expected = 0;
                actual = 0;
                return false;
            }

            expected = check.Hash;
            WorldData snapshot = _history.GetSnapshot(step < 0 ? int.MinValue : checked((int)step));
            actual = SkyPirates.Shared.Tools.Json.SerializationUTF8.GetHashSum(snapshot);
            return actual == expected;
        }

        private void ResetSession()
        {
            SkyPiratesReplayData replay = _replay!;
            _history.ApplySnapshot(replay.InitialWorld);
            _history.SaveBaseline();

            long firstSimulatedStep = checked(replay.InitialStep + 1);
            var start = new SessionStart(
                new SessionId(1),
                new AuthorId(0),
                new ProtocolId(1),
                firstSimulatedStep,
                0);
            _session.Restart(start);

            for (int i = 0; i < replay.Commands.Count; i++)
            {
                SkyPiratesReplayCommand command = replay.Commands[i];
                var key = new CommandKey(start.SessionId, new AuthorId(command.AuthorId), command.Sequence);
                var header = new CommandHeader(key, command.TypeId, command.Step, command.Order);
                _session.ApplyReplayCommand(header, command.Payload);
            }
        }

        private void RestoreNearestCheckpoint(long targetStep)
        {
            SessionSnapshot? checkpoint = null;
            foreach (KeyValuePair<long, SessionSnapshot> item in _checkpoints)
            {
                if (item.Key > targetStep)
                    break;

                checkpoint = item.Value;
            }

            if (checkpoint == null)
            {
                ResetSession();
                return;
            }

            _session.Session.Restore(checkpoint);
            SkyPiratesReplayData replay = _replay!;
            for (int i = 0; i < replay.Commands.Count; i++)
            {
                SkyPiratesReplayCommand command = replay.Commands[i];
                if (command.Step <= checkpoint.Step)
                    continue;

                var key = new CommandKey(_session.Session.Start.SessionId, new AuthorId(command.AuthorId), command.Sequence);
                var header = new CommandHeader(key, command.TypeId, command.Step, command.Order);
                _session.ApplyReplayCommand(header, command.Payload);
            }
        }

        private void CacheCheckpoint(SessionSnapshot checkpoint)
        {
            _checkpoints[checkpoint.Step] = checkpoint;
            while (_checkpoints.Count > MaximumCachedCheckpoints)
            {
                long oldestStep = long.MaxValue;
                foreach (long step in _checkpoints.Keys)
                {
                    oldestStep = step;
                    break;
                }

                if (oldestStep == long.MaxValue)
                    break;

                _checkpoints.Remove(oldestStep);
            }
        }
    }
}
