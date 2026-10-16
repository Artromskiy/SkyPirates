using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace SlyPirates.Shared.Services
{
    public class TimelineWriter : ITickableExecutor
    {
        private readonly IWorldHistory _history;
        private readonly Dictionary<int, WorldData> _timeline = new();

        public TimelineWriter(IWorldHistory history) => _history = history;

        public void Tick(int tick)
        {
            int snapshotTick = _history.OldestTick;
            var snapshot = _history.GetSnapshot(snapshotTick);
            _timeline[snapshotTick] = snapshot;
        }

        public Dictionary<int, WorldData> GetSnapshots() => _timeline;
    }
}
