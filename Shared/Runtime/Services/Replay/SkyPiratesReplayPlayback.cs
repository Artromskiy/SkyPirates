using Delta.Netcode;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.IServices.TickableExecutors;
using SkyPirates.Shared.Services.Netcode;
using System;

namespace SkyPirates.Shared.Services.Replay
{
    public sealed class SkyPiratesReplayPlayback
    {
        private readonly IHistorySystem _history;
        private readonly SkyPiratesSessionProvider _session;
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
            ResetSession();
        }

        public void Seek(long targetStep)
        {
            if (_replay == null)
                throw new InvalidOperationException("Load a replay before seeking.");
            if (targetStep < _replay.InitialStep || targetStep > LastStep)
                throw new ArgumentOutOfRangeException(nameof(targetStep));

            if (targetStep < _session.CurrentStep)
                ResetSession();

            if (targetStep > _session.CurrentStep)
                _session.Session.Tick(targetStep);
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
    }
}
