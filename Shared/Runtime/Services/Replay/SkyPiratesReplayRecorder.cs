using Delta.Netcode;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Services.Netcode;
using DVG.SkyPirates.Shared.Tools.Json;
using System;
using System.Linq;

namespace DVG.SkyPirates.Shared.Services.Replay
{
    public sealed class SkyPiratesReplayRecorder
    {
        private readonly SessionHost _session;
        private readonly IHistorySystem _history;
        private readonly CommandCursor _commandCursor;
        private readonly SkyPiratesReplayData _replay;

        public SkyPiratesReplayRecorder(SessionHost session, IHistorySystem history, WorldData initialWorld, long initialStep)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _replay = new SkyPiratesReplayData
            {
                InitialWorld = initialWorld,
                InitialStep = initialStep,
            };

            // All commands at the initial frame are already represented by InitialWorld.
            _commandCursor = new CommandCursor(initialStep, uint.MaxValue);
        }

        public SkyPiratesReplayData Capture()
        {
            _replay.Commands.Clear();
            var records = _session.ReadAcceptedAfter(_commandCursor).ToArray();
            long currentStep = _session.CurrentStep;
            for (int i = 0; i < records.Length; i++)
            {
                JournalRecord record = records[i];
                if (record.Result != CommandResult.Accepted || record.IsCancelled ||
                    record.Header.Step <= _replay.InitialStep || record.Header.Step > currentStep)
                {
                    continue;
                }

                var key = record.Header.Key;
                _replay.Commands.Add(new SkyPiratesReplayCommand
                {
                    AuthorId = key.AuthorId.Value,
                    Sequence = key.Sequence,
                    TypeId = record.Header.TypeId,
                    Step = record.Header.Step,
                    Order = record.Header.Order,
                    Payload = record.FinalPayload.ToArray(),
                });
            }

            if (currentStep >= _replay.InitialStep)
            {
                WorldData currentWorld = currentStep < 0
                    ? _replay.InitialWorld
                    : _history.GetSnapshot(checked((int)currentStep));
                _replay.EndHashCheck = new SkyPiratesReplayHashCheck
                {
                    Step = currentStep,
                    Hash = SerializationUTF8.GetHashSum(currentWorld),
                };
            }

            return _replay;
        }
    }
}
