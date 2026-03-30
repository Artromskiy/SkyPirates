using DVG.SkyPirates.Client.DI;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.Services;
using DVG.SkyPirates.Shared.Tools.Json;
using System.Buffers;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace DVG.SkyPirates.Client.Views
{
    [Inject]
    public class ShareTimelineView : MonoBehaviour
    {
        [SerializeField]
        private Button _button;

        [Inject]
        private readonly TimelineWriter _writer;
        [Inject]
        private readonly ICommandExecutorService _commands;

        private void Start()
        {
            _button.onClick.AddListener(Save);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                var path = GetPath("Snapshots");
                Save(path, GetObj());
            }
        }

        private void Save()
        {
            var path = Path.GetTempFileName();
            Save(path, GetObj());
            new NativeShare().AddFile(path).Share();
        }

        private object GetObj()
        {
            var snaphsots = _writer.GetSnapshots();
            var commands = _commands.GetCommands();
            return (snaphsots, commands);
        }

        private void Save(string path, object obj)
        {
            ArrayBufferWriter<byte> buffer = new();
            SerializationUTF8.SerializeCompressed(obj, buffer);
            using var file = File.Open(path, FileMode.Create, FileAccess.ReadWrite);
            file.Write(buffer.WrittenSpan);
        }

        private string GetPath(string fileName)
        {
            const string folder = "Scripts/SkyPirates/Tooling/TimelineDebug";
            var path = Path.Combine(Application.dataPath, folder, fileName);
            path = Path.ChangeExtension(path, "txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return path;
        }
    }
}