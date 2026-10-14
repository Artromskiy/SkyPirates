using DVG.Collections;
using DVG.Components;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Services
{
    public class TimelineWriter : ITickableExecutor
    {
        private static readonly Action<WorldData>[] TrimActions = CreateTrimActions();
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
            for (int i = 0; i < TrimActions.Length; i++)
            {
                TrimActions[i](snapshot);
            }

            _timeline[snapshotTick] = snapshot;
        }

        public Dictionary<int, WorldData> GetSnapshots() => _timeline;

        private static Action<WorldData>[] CreateTrimActions()
        {
            var actions = new List<Action<WorldData>>();
            var collectActions = new CollectTrimActions(actions);
            HistoryComponentsRegistry.ForEachData(ref collectActions);
            return actions.ToArray();
        }

        private static void TrimExcess<T>(WorldData data) where T : struct
            => data.Get<T>().TrimExcess();

        private readonly struct CollectTrimActions : IStructGenericAction
        {
            private readonly List<Action<WorldData>> _actions;

            public CollectTrimActions(List<Action<WorldData>> actions)
            {
                _actions = actions;
            }

            public void Invoke<T>() where T : struct
                => _actions.Add(TrimExcess<T>);
        }
    }
}
