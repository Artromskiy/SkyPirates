using SkyPirates.Shared.IServices;
using SkyPirates.Shared.IServices.TickableExecutors;
using SkyPirates.Shared.Tools.Json;
using System;
using System.Threading.Tasks;

namespace SkyPirates.Shared.Services
{
    public class HashSumService : IHashSumService
    {
        private readonly IHistorySystem _historySystem;

        public HashSumService(IHistorySystem historySystem)
        {
            _historySystem = historySystem;
        }

        public void Tick(int tick)
        {
            var worldData = _historySystem.GetSnapshot(tick);
            Task.Run(() => Delta.Diagnostics.Trace.Info(SerializationUTF8.GetHashSum(worldData).ToString()));
        }
    }
}
