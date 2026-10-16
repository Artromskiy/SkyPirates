using DVG.SkyPirates.Shared.Core.Collections;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Services
{
    public class TimelineWriter : ITickableExecutor
    {
        private readonly IHistorySystem _historySystem;
        private readonly Dictionary<int, WorldData> _timeline = new();

        public TimelineWriter(IHistorySystem historySystem) => _historySystem = historySystem;

        public void Tick(int tick)
        {
            int snapshotTick = tick - Constants.MaxHistoryTicks + 1;
            var snapshot = _historySystem.GetSnapshot(snapshotTick);
            _timeline[snapshotTick] = snapshot;
        }

        public Dictionary<int, WorldData> GetSnapshots() => _timeline;
    }
}
