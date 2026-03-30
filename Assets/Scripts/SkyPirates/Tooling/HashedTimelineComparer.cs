using DVG.Commands;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Tools.Json;
using NaughtyAttributes;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Threading.Tasks;
using UnityEngine;
using HashedTimeline = System.ValueTuple<System.Collections.Generic.Dictionary<int, DVG.SkyPirates.Shared.Data.WorldData>, DVG.SkyPirates.Shared.Commands.CommandsData>;

namespace DVG.SkyPirates.Tooling
{
    public class HashedTimelineComparer : MonoBehaviour
    {
        [SerializeField]
        private TextAsset[] _assets;

        [SerializeField]
        private int _tick;
        [SerializeField]
        private int _timelineIndex;
        [SerializeField]
        private bool _loaded;
        [SerializeField]
        private WorldDataInfo[] _infos;

        private HashedTimeline[] _timelines;

        [Button]
        private void Next()
        {
            _tick++;
            Compare();
        }

        [Button]
        private void Prev()
        {
            _tick--;
            Compare();
        }

        [Button]
        private void Reload()
        {
            _loaded = false;
            _timelines = new HashedTimeline[_assets.Length];
            Task[] tasks = new Task[_timelines.Length];
            var bytes = new byte[_timelines.Length][];
            _infos = new WorldDataInfo[_timelines.Length];
            for (int i = 0; i < _timelines.Length; i++)
                bytes[i] = _assets[i].bytes;

            for (int i = 0; i < _timelines.Length; i++)
            {
                int index = i;
                tasks[index] = Task.Run(() =>
                {
                    _timelines[index] = SerializationUTF8.
                        DeserializeCompressed<HashedTimeline>(bytes[index]);
                });
            }
            Task.WhenAll(tasks).ContinueWith((_) => _loaded = true);
        }

        public void Compare()
        {
            if (!_loaded)
                return;
            for (int i = 0; i < _timelines.Length; i++)
            {
                var data = Get(i).Item1;
                var worldData = data?.GetValueOrDefault(_tick);
                _infos[i].Contains = worldData is not null;
                _infos[i].Hash = worldData is null ? 0 : (int)GetWorldHash(worldData);
            }
        }

        [Button]
        public void ExportWorld()
        {
            if (!_loaded)
                return;
            var res = SerializationUTF8.SerializeOrdered(Get(_timelineIndex).Item1[_tick]);
            File.WriteAllText(GetPath("Snapshot"), res);
        }

        [Button]
        public void ExportCommands()
        {
            if (!_loaded)
                return;
            var commands = Get(_timelineIndex).Item2;
            var atTick = GetAtTick(commands, _tick);
            var res = SerializationUTF8.SerializeOrdered(atTick);
            Console.WriteLine(res);
            File.WriteAllText(GetPath("Command"), res);
        }

        [Button]
        public void ExportCommandsFull()
        {
            if (!_loaded)
                return;
            var commands = Get(_timelineIndex).Item2;
            var res = SerializationUTF8.SerializeOrdered(commands);
            Console.WriteLine(res);
            File.WriteAllText(GetPath("CommandsData"), res);
        }

        private string GetPath(string fileName)
        {
            const string folder = "Scripts/SkyPirates/Tooling/TimelineDebug";
            var path = Path.Combine(Application.dataPath, folder, fileName);
            path = Path.ChangeExtension(path, "json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return path;
        }

        private HashedTimeline Get(int index)
        {
            index = Maths.Clamp(index, 0, _assets.Length);
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
            CommandsRegistry.ForEach(ref copyAction);
            return copy;
        }

        private CommandsData GetAtTick(CommandsData commandsData, int tick)
        {
            var copy = Copy(commandsData);
            var removeAction = new RemoveAction(copy, tick);
            CommandsRegistry.ForEach(ref removeAction);
            return copy;
        }

        private readonly struct RemoveAction : IGenericAction
        {
            private readonly CommandsData _source;
            private readonly int _tick;

            public RemoveAction(CommandsData source, int tick)
            {
                _source = source;
                _tick = tick;
            }

            public void Invoke<T>()
            {
                List<int> keys = new(_source.Get<T>().Keys);
                foreach (var item in keys)
                {
                    if (item != _tick)
                        _source.Get<T>().Remove(item);
                }
            }
        }
        private readonly struct CommandsCopyAction : IGenericAction
        {
            private readonly CommandsData _orig;
            private readonly CommandsData _copy;

            public CommandsCopyAction(CommandsData orig, CommandsData copy)
            {
                _orig = orig;
                _copy = copy;
            }

            public readonly void Invoke<T>()
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
