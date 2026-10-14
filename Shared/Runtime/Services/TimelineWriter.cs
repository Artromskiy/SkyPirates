using SkyPirates.Shared.Core;
using SkyPirates.Shared.Core.Collections;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;

namespace SkyPirates.Shared.Services
{
    public class TimelineWriter : ITickableExecutor
    {
        private readonly IHistorySystem _historySystem;
        private readonly Dictionary<int, WorldData> _timeline = new();

        public TimelineWriter(IHistorySystem historySystem)
        {
            _historySystem = historySystem;
        }

        public void Tick(int tick)
        {
            int snapshotTick = tick - Constants.MaxHistoryTicks + 1;
            var snapshot = _historySystem.GetSnapshot(snapshotTick);
            snapshot.TrimExcess();

            _timeline[snapshotTick] = snapshot;
        }

        public Dictionary<int, WorldData> GetSnapshots() => _timeline;
    }
}
