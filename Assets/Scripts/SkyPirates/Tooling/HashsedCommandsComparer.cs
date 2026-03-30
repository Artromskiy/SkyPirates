using DVG.Commands;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Tools.Json;
using NaughtyAttributes;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DVG.SkyPirates.Tooling
{
    public class HashsedCommandsComparer : MonoBehaviour
    {
        [SerializeField]
        private TextAsset _left;
        [SerializeField]
        private TextAsset _righ;

        [SerializeField]
        private int _tick;

        public void Log()
        {

        }

        [Button]
        public void ExportLeft()
        {
            var left = GetLeft();
            AtTick(left, _tick);
            var res = SerializationUTF8.SerializeOrdered(left);
            File.WriteAllText(GetPath(), res);
        }

        [Button]
        public void ExportRight()
        {
            var right = GetRight();
            AtTick(right, _tick);
            var res = SerializationUTF8.SerializeOrdered(right);
            File.WriteAllText(GetPath(), res);
        }

        private string GetPath()
        {
            const string folder = "Scripts/SkyPirates/Tooling/TimelineDebug";
            const string fileName = "Command";
            var path = Path.Combine(Application.dataPath, folder, fileName);
            path = Path.ChangeExtension(path, "json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return path;
        }

        private CommandsData GetLeft()
        {
            return SerializationUTF8.DeserializeCompressed<CommandsData>(_left.bytes);
        }

        private CommandsData GetRight()
        {
            return SerializationUTF8.DeserializeCompressed<CommandsData>(_righ.bytes);
        }

        private void AtTick(CommandsData commandsData, int tick)
        {
            RemoveAction removeAction = new(commandsData, tick);
            CommandsRegistry.ForEach(ref removeAction);
        }

        private readonly struct RemoveAction : IGenericAction
        {
            private readonly CommandsData _commandsData;
            private readonly int _tick;

            public RemoveAction(CommandsData commandsData, int tick)
            {
                _commandsData = commandsData;
                _tick = tick;
            }

            public void Invoke<T>()
            {
                var commands = _commandsData.Get<T>();
                foreach (var tick in commands.Keys.ToArray())
                {
                    if (tick != _tick)
                        commands.Remove(tick);
                }
            }
        }
    }
}
