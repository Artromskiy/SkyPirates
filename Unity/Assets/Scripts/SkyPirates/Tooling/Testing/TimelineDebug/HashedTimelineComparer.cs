using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Threading.Tasks;
using Delta;
using Delta.Netcode;
using SkyPirates.Shared.Commands;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.Tools.Json;
using UnityEngine;

namespace SkyPirates.Tooling.Testing
{
    public class HashedTimelineComparer : MonoBehaviour
    {
        [SerializeField]
        private TextAsset[] _assets = Array.Empty<TextAsset>();

        [SerializeField]
        private int _tick;
        [SerializeField]
        private int _timelineIndex;
        [SerializeField]
        private bool _loaded;
        [SerializeField]
        private WorldDataInfo[] _infos = Array.Empty<WorldDataInfo>();

        private (Dictionary<int, WorldData> WorldData, CommandsData Commands)[] _timelines = Array.Empty<(Dictionary<int, WorldData>, CommandsData)>();
        private bool _isLoading;

        [ContextMenu("Timeline/Next")]
        private void Next()
        {
            _tick++;
            Compare();
        }

        [ContextMenu("Timeline/Previous")]
        private void Prev()
        {
            _tick--;
            Compare();
        }

        [ContextMenu("Timeline/Reload")]
        private async void Reload()
        {
            if (_isLoading)
            {
                return;
            }

            _isLoading = true;
            try
            {
                _loaded = false;
                _timelines = Array.Empty<(Dictionary<int, WorldData>, CommandsData)>();
                _infos = Array.Empty<WorldDataInfo>();
                if (_assets == null || _assets.Length == 0)
                {
                    Debug.LogWarning("Assign at least one timeline asset before reloading.", this);
                    return;
                }

                var timelines = new (Dictionary<int, WorldData> WorldData, CommandsData Commands)[_assets.Length];
                Task[] tasks = new Task[timelines.Length];
                byte[][] bytes = new byte[timelines.Length][];
                for (int i = 0; i < timelines.Length; i++)
                {
                    if (_assets[i] == null)
                    {
                        Debug.LogError($"Timeline asset at index {i} is missing.", this);
                        return;
                    }

                    bytes[i] = _assets[i].bytes;
                }

                for (int i = 0; i < timelines.Length; i++)
                {
                    int index = i;
                    tasks[index] = Task.Run(() =>
                    {
                        timelines[index] = SerializationUTF8.
                            DeserializeCompressed<(Dictionary<int, WorldData> WorldData, CommandsData Commands)>(bytes[index]);
                    });
                }

                await Task.WhenAll(tasks);
                _timelines = timelines;
                _infos = new WorldDataInfo[timelines.Length];
                _loaded = true;
                Compare();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                _isLoading = false;
            }
        }

        public void Compare()
        {
            if (!_loaded)
            {
                return;
            }

            for (int i = 0; i < _timelines.Length; i++)
            {
                var data = Get(i).WorldData;
                var worldData = data?.GetValueOrDefault(_tick);
                _infos[i].Contains = worldData is not null;
                _infos[i].Hash = worldData is null ? 0 : (int)GetWorldHash(worldData);
            }
        }

        [ContextMenu("Timeline/Export World")]
        public void ExportWorld()
        {
            if (!_loaded)
            {
                return;
            }

            if (!Get(_timelineIndex).WorldData.TryGetValue(_tick, out WorldData worldData))
            {
                Debug.LogWarning($"Timeline {_timelineIndex} has no world snapshot for tick {_tick}.", this);
                return;
            }

            string res = SerializationUTF8.SerializeOrdered(worldData);
            File.WriteAllText(ToolingOutput.GetJsonPath("TimelineDebug", "Snapshot"), res);
        }

        [ContextMenu("Timeline/Export Commands at Tick")]
        public void ExportCommands()
        {
            if (!_loaded)
            {
                return;
            }

            var commands = Get(_timelineIndex).Commands;
            var atTick = GetAtTick(commands, _tick);
            string res = SerializationUTF8.SerializeOrdered(atTick);
            Delta.Diagnostics.Trace.Info(res);
            File.WriteAllText(ToolingOutput.GetJsonPath("TimelineDebug", "Command"), res);
        }

        [ContextMenu("Timeline/Export All Commands")]
        public void ExportCommandsFull()
        {
            if (!_loaded)
            {
                return;
            }

            var commands = Get(_timelineIndex).Commands;
            string res = SerializationUTF8.SerializeOrdered(commands);
            Delta.Diagnostics.Trace.Info(res);
            File.WriteAllText(ToolingOutput.GetJsonPath("TimelineDebug", "CommandsData"), res);
        }

        private (Dictionary<int, WorldData> WorldData, CommandsData Commands) Get(int index)
        {
            if (_timelines.Length == 0)
                throw new InvalidOperationException("Reload at least one timeline before selecting a frame.");

            index = Maths.Clamp(index, 0, _timelines.Length - 1);
            return _timelines[index];
        }

        private ulong GetWorldHash(WorldData worldData)
        {
            ArrayBufferWriter<byte> writer = new();
            SerializationUTF8.SerializeOrdered(worldData, writer);
            return XxHash64.HashToUInt64(writer.WrittenSpan);
        }

        private CommandsData Copy(CommandsData commandsData)
        {
            var copy = new CommandsData();
            var copyAction = new CommandsCopyAction(commandsData, copy);
            foreach (ICommandRegistration registration in GeneratedCommands.Registrations)
                registration.Visit(ref copyAction);
            return copy;
        }

        private CommandsData GetAtTick(CommandsData commandsData, int tick)
        {
            var copy = Copy(commandsData);
            var removeAction = new RemoveAction(copy, tick);
            foreach (ICommandRegistration registration in GeneratedCommands.Registrations)
                registration.Visit(ref removeAction);
            return copy;
        }

        private readonly struct RemoveAction : ICommandVisitor
        {
            private readonly CommandsData _source;
            private readonly int _tick;

            public RemoveAction(CommandsData source, int tick)
            {
                _source = source;
                _tick = tick;
            }

            public void Visit<T>()
            {
                List<int> keys = new(_source.Get<T>().Keys);
                foreach (int item in keys)
                {
                    if (item != _tick)
                    {
                        _source.Get<T>().Remove(item);
                    }
                }
            }
        }

        private readonly struct CommandsCopyAction : ICommandVisitor
        {
            private readonly CommandsData _orig;
            private readonly CommandsData _copy;

            public CommandsCopyAction(CommandsData orig, CommandsData copy)
            {
                _orig = orig;
                _copy = copy;
            }

            public readonly void Visit<T>()
            {
                foreach (var item in _orig.Get<T>())
                {
                    _copy.Get<T>().Add(item.Key, item.Value);
                }
            }
        }


        [Serializable]
        private struct WorldDataInfo
        {
            public bool Contains;
            public int Hash;
        }
    }
}
