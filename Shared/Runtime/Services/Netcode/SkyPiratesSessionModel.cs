using Delta.Netcode;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.Tools.Json;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Services.Netcode
{
    public sealed class SkyPiratesSessionModel : ISessionModel
    {
        private readonly IWorldHistory _history;
        private readonly IDeltaTickableService<IDeltaTickableExecutor> _systems;
        private readonly ITickableService<IInTickable> _inTickables;
        private readonly IEntityRegistry _entityRegistry;
        private readonly Dictionary<long, List<CommandEntry>> _commands = new();
        private readonly Dictionary<CommandKey, long> _commandSteps = new();
        private long _dirtyStep = long.MaxValue;

        public SkyPiratesSessionModel(
            long startStep,
            IWorldHistory history,
            IDeltaTickableService<IDeltaTickableExecutor> systems,
            ITickableService<IInTickable> inTickables,
            IEntityRegistry entityRegistry)
        {
            CurrentStep = startStep - 1;
            ComputedStep = CurrentStep;
            _history = history;
            _systems = systems;
            _inTickables = inTickables;
            _entityRegistry = entityRegistry;
        }

        public event Action? StateRestored;
        public long CurrentStep { get; private set; }
        public long ComputedStep { get; private set; }
        public bool HasDirtyCommands => _dirtyStep <= ComputedStep;
        public bool CommandsLocked { get; private set; }
        public void LockCommands() => CommandsLocked = true;
        private void EnsureMutableCommands()
        {
            if (CommandsLocked) throw new InvalidOperationException("Recorded replay commands are immutable.");
        }

        public bool TrySchedule(ref long simulationStep)
        {
            long oldestStep = Math.Max(0, (long)_history.OldestTick + 1);
            return !CommandsLocked && simulationStep >= oldestStep;
        }

        public bool CanCancel(in CommandHeader header)
            => !CommandsLocked && header.Step > _history.OldestTick;

        public void SetCommand(CommandEntry command)
        {
            EnsureMutableCommands();
            if (command.Header.Step <= _history.OldestTick)
                throw new InvalidOperationException("The command is older than the available rollback range.");
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
            if (command.Header.Step <= ComputedStep)
            {
                _dirtyStep = Math.Min(_dirtyStep, command.Header.Step);
            }
        }

        public void Remove(CommandKey key)
        {
            EnsureMutableCommands();
            if (_commandSteps.TryGetValue(key, out long scheduled) && scheduled <= _history.OldestTick)
                throw new InvalidOperationException("The command is older than the available rollback range.");
            if (!RemoveScheduledCommand(key, out long step))
            {
                return;
            }

            if (step <= ComputedStep)
            {
                _dirtyStep = Math.Min(_dirtyStep, step);
            }
        }

        public void Tick(long simulationStep)
        {
            if (simulationStep < _history.OldestTick)
                throw new ArgumentOutOfRangeException(nameof(simulationStep));
            if (_dirtyStep <= ComputedStep)
            {
                long restoreStep = _dirtyStep - 1;
                _history.Rollback(checked((int)restoreStep));
                CurrentStep = ComputedStep = restoreStep;
                _entityRegistry.Rebuild();
                StateRestored?.Invoke();
                _dirtyStep = long.MaxValue;
            }
            if (simulationStep <= ComputedStep)
            {
                RestoreFixedState(simulationStep);
                return;
            }
            if (CurrentStep != ComputedStep) RestoreFixedState(ComputedStep);
            while (CurrentStep < simulationStep)
            {
                long step = CurrentStep + 1;
                Simulate(step);
                CurrentStep = ComputedStep = step;
            }
        }

        public void RestoreFixedState(long step)
        {
            bool moved = step != CurrentStep;
            _history.GoTo(checked((int)step));
            CurrentStep = step;
            _entityRegistry.Rebuild();
            if (moved) StateRestored?.Invoke();
        }

        public void ApplySnapshot(WorldData snapshot, int tick)
        {
            _history.ApplySnapshot(snapshot, tick);
            _entityRegistry.Rebuild();
            CurrentStep = ComputedStep = tick;
            _dirtyStep = long.MaxValue;
            StateRestored?.Invoke();
        }

        public void Save(IBufferWriter<byte> output)
        {
            long snapshotStep = CurrentStep;
            var state = new SnapshotState
            {
                Step = snapshotStep,
                World = _history.GetSnapshot(checked((int)snapshotStep)),
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
            ApplySnapshot(snapshot.World, checked((int)snapshot.Step));
            _commands.Clear();
            _commandSteps.Clear();
        }

        private void Simulate(long step)
        {
            if (_commands.TryGetValue(step, out var commands))
            {
                for (int index = 0; index < commands.Count; index++)
                {
                    _entityRegistry.Synchronize();
                    commands[index].Execute();
                }
            }

            int tick = checked((int)step);
            _systems.Tick(tick, Constants.TickTime);
            _entityRegistry.Synchronize();
            _history.Save(tick);
            _inTickables.Tick(tick);
        }

        private void RemoveExisting(CommandKey key)
        {
            if (!RemoveScheduledCommand(key, out long oldStep))
            {
                return;
            }

            if (oldStep <= ComputedStep)
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
