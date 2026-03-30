using DVG.SkyPirates.Client.DI;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Tools.Json;
using NaughtyAttributes;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace DVG.SkyPirates.Client.Views
{
    public class WorldSerializerView : MonoBehaviour
    {
        [SerializeField]
        private string _folder = "Scripts/SkyPirates/Shared/Resources/Configs/Maps";
        [SerializeField]
        private string _mapName = "Map1";

        [Inject]
        private readonly IHistorySystem _historySystem;
        [Inject]
        private readonly ITickCounterService _tickCounterService;

        [Button]
        public void Serialize()
        {
            Profiler.BeginSample("SerializeMap");
            var worldData = _historySystem.GetSnapshot(_tickCounterService.TickCounter);
            var _serializedText = SerializationUTF8.Serialize(worldData);
            var path = Path.Combine(Application.dataPath, _folder, _mapName);
            path = Path.ChangeExtension(path, "json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, _serializedText);
            Profiler.EndSample();
        }
    }
}
