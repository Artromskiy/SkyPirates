using Arch.Core;
using DVG.SkyPirates.Client.DI;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using SimpleInjector;
using System.Diagnostics;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.Entry
{
    public class ToolingStart : MonoBehaviour
    {
        private Container _container = null!;

        private readonly Stopwatch _mainSw = new();

        private void Awake()
        {
            _container = new LocalContainer();
            _container.RegisterAndInjectViewModels();
            _container.GetInstance<World>().Clear();
        }

        public void Update()
        {
            _mainSw.Start();
            int tickFrame = (int)(_mainSw.Elapsed.Ticks * Constants.TicksPerSecond / 10_000_000);
            var timeline = _container.GetInstance<ITimelineService>();
            var preTickable = _container.GetInstance<ITickableService<IPreTickable>>();
            var postTickable = _container.GetInstance<ITickableService<IPostTickable>>();
            var targetTick = tickFrame;
            if (timeline.CurrentTick != targetTick)
            {
                preTickable.Tick(targetTick);
                timeline.Tick(targetTick);
                postTickable.Tick(targetTick);
            }
        }
    }
}
