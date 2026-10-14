using Delta.Netcode;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Tools.Json;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Services.Netcode
{
    public sealed class SkyPiratesSessionModel : ISessionModel
    {
        private readonly IHistorySystem _history;
        private readonly IDeltaTickableService<IDeltaTickableExecutor> _systems;
        private readonly ITickableService<IInTickable> _inTickables;
        private readonly IDisposeSystem _disposeSystem;
        private readonly Dictionary<long, List<CommandEntry>> _commands = new();
        private readonly Dictionary<CommandKey, long> _commandSteps = new();
        private long _dirtyStep = long.MaxValue;

        public SkyPiratesSessionModel(
            long startStep,
            IHistorySystem history,
            IDeltaTickableService<IDeltaTickableExecutor> systems,
            ITickableService<IInTickable> inTickables,
            IDisposeSystem disposeSystem)
        {
            CurrentStep = startStep - 1;
            _history = history;
            _systems = systems;
            _inTickables = inTickables;
            _disposeSystem = disposeSystem;
        }

        public long CurrentStep { get; private set; }

        public bool TrySchedule(ref long simulationStep)
        {
            long oldestStep = Math.Max(0, CurrentStep - Constants.MaxHistoryTicks + 1);
            return simulationStep >= oldestStep;
        }

        public bool CanCancel(in CommandHeader header)
            => header.Step > CurrentStep - Constants.MaxHistoryTicks;

        public void SetCommand(CommandEntry command)
        {
            RemoveExisting(command.Header.Key);

            if (!_commands.TryGetValue(command.Header.Step, out var commands))
            {
                _commands.Add(command.Header.Step, commands = new List<CommandEntry>());
            }

            int index = commands.FindIndex(existing => existing.Header.Order > command.Header.Order);
            if (index < 0)
            {
                commands.Add(command);
            }
            else
            {
                commands.Insert(index, command);
            }

            _commandSteps[command.Header.Key] = command.Header.Step;
            if (command.Header.Step <= CurrentStep)
            {
                _dirtyStep = Math.Min(_dirtyStep, command.Header.Step);
            }
        }

        public void Remove(CommandKey key)
        {
            if (!RemoveScheduledCommand(key, out long step))
            {
                return;
            }

            if (step <= CurrentStep)
            {
                _dirtyStep = Math.Min(_dirtyStep, step);
            }
        }

        public void Tick(long simulationStep)
        {
            if (_dirtyStep <= CurrentStep)
            {
                long restoreStep = _dirtyStep - 1;
                _history.Rollback(restoreStep < 0 ? int.MinValue : checked((int)restoreStep));
                CurrentStep = restoreStep;
                _dirtyStep = long.MaxValue;
            }

            while (CurrentStep < simulationStep)
            {
                Simulate(++CurrentStep);
            }
        }

        public void Save(IBufferWriter<byte> output)
        {
            long snapshotStep = CurrentStep;
            var state = new SnapshotState
            {
                Step = snapshotStep,
                World = _history.GetSnapshot(snapshotStep < 0 ? int.MinValue : checked((int)snapshotStep)),
            };
            SerializationUTF8.SerializeCompressed(state, output);
        }

        public long SaveReplayAnchor(IBufferWriter<byte> output)
        {
            Save(output);
            return CurrentStep;
        }

        public void Load(ReadOnlySpan<byte> state)
        {
            var snapshot = SerializationUTF8.DeserializeCompressed<SnapshotState>(state.ToArray());
            _history.ApplySnapshot(snapshot.World);
            _history.SaveBaseline();
            CurrentStep = snapshot.Step;
            _dirtyStep = long.MaxValue;
            _commands.Clear();
            _commandSteps.Clear();
        }

        private void Simulate(long step)
        {
            if (_commands.TryGetValue(step, out var commands))
            {
                for (int index = 0; index < commands.Count; index++)
                {
                    commands[index].Execute();
                }
            }

            int tick = checked((int)step);
            _systems.Tick(tick, Constants.TickTime);
            _disposeSystem.Tick(tick);
            _history.Save(tick);
            _inTickables.Tick(tick);
        }

        private void RemoveExisting(CommandKey key)
        {
            if (!RemoveScheduledCommand(key, out long oldStep))
            {
                return;
            }

            if (oldStep <= CurrentStep)
            {
                _dirtyStep = Math.Min(_dirtyStep, oldStep);
            }
        }

        private bool RemoveScheduledCommand(CommandKey key, out long step)
        {
            if (!_commandSteps.TryGetValue(key, out step))
            {
                return false;
            }

            if (_commands.TryGetValue(step, out var commands))
            {
                commands.RemoveAll(command => command.Header.Key.Equals(key));
                if (commands.Count == 0)
                {
                    _commands.Remove(step);
                }
            }

            _commandSteps.Remove(key);
            return true;
        }

        private struct SnapshotState
        {
            public long Step;
            public WorldData World;
        }
    }
}
