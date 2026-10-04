using Delta;
using Delta.Netcode;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Diagnostics;
using CommandsRegistry = DVG.Commands.CommandsRegistry;
using IGenericAction = DVG.IGenericAction;

namespace DVG.SkyPirates.Client.Entry
{
    public class GameStartController
    {
        private readonly ITimelineService _timeline;
        private readonly ITickableService<IPreTickable> _preTickableService;
        private readonly ITickableService<IPostTickable> _postTickableService;
        private readonly ICommandReciever _comandReciever;

        private readonly Stopwatch _mainSw = new();
        private int TickOffset = 0;
        private int _targetTick;

        public GameStartController(ITimelineService timeline, ITickableService<IPreTickable> preTickableService, ITickableService<IPostTickable> postTickableService, ICommandReciever comandReciever)
        {
            _timeline = timeline;
            _preTickableService = preTickableService;
            _postTickableService = postTickableService;
            _comandReciever = comandReciever;
            _comandReciever.RegisterReciever<TickSyncCommand>(OnSyncTick);
            _comandReciever.RegisterReciever<LoadWorldCommand>(c =>
            {
                _timeline.CurrentTick = SkyPiratesCommand.GetTick(c);
                _timeline.DirtyTick = SkyPiratesCommand.GetTick(c);
            });
            var subscrive = new CommandCallback(_timeline, _comandReciever);
            CommandsRegistry.ForEach(ref subscrive);
        }

        private void OnSyncTick(Command<TickSyncCommand> cmd)
        {
            if (SkyPiratesCommand.GetTick(cmd) > _targetTick)
            {
                TickOffset = Maths.Max(SkyPiratesCommand.GetTick(cmd), TickOffset);
                _mainSw.Restart();
            }
        }

        public void Update()
        {
            _mainSw.Start();
            _targetTick = TickOffset + (int)(_mainSw.Elapsed.Ticks * Constants.TicksPerSecond / 10_000_000);

            if (_timeline.CurrentTick != _targetTick)
            {
                _preTickableService.Tick(_targetTick);
                _timeline.Tick(_targetTick);
                _postTickableService.Tick(_targetTick);
            }
        }


        private readonly struct CommandCallback : IGenericAction
        {
            private readonly ITimelineService _timelineService;
            private readonly ICommandReciever _commandReciever;

            public CommandCallback(ITimelineService timelineService, ICommandReciever commandReciever)
            {
                _timelineService = timelineService;
                _commandReciever = commandReciever;
            }

            public void Invoke<T>()
            {
                var timeline = _timelineService;
                _commandReciever.RegisterReciever<T>((c) =>
                {
                    timeline.DirtyTick = Maths.Min(timeline.DirtyTick, SkyPiratesCommand.GetTick(c));
                });
            }
        }
    }
}
