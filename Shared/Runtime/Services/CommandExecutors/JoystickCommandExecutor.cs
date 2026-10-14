using Delta;
using Delta.ECS;
using SkyPirates.Shared.Ecs;
using Delta.Netcode;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Commands;
using SkyPirates.Shared.IServices;
using SkyPirates.Shared.Systems;
using SkyPirates.Shared.Tools.Extensions;
using SkyPirates.Shared.Tools.TraceHelpers;
using System.Diagnostics;
using System;

namespace SkyPirates.Shared.Services.CommandExecutors
{
    public class JoystickCommandExecutor : ICommandExecutorRegistration, ICommandExecutor<JoystickCommand>, ITransientCommandInput<JoystickCommand>
    {
        private readonly IEntityRegistry _entityRegistryService;
        private readonly World _world;

        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<SquadMember>().NotDisabled().Alive();

        public JoystickCommandExecutor(IEntityRegistry entityRegistryService, World world)
        {
            _entityRegistryService = entityRegistryService;
            _world = world;
        }

        public void Execute(in Command<JoystickCommand> cmd)
        {
            Apply(cmd.Payload);
        }

        public void ApplyTransient(in JoystickCommand input)
        {
            Apply(input);
        }

        private void Apply(JoystickCommand input)
        {
            _entityRegistryService.TryGet(input.Target, out var squad);

            if (squad == default ||
                !_world.IsAlive(squad) ||
                !_world.Has<Alive>(squad))
            {
                Delta.Diagnostics.Trace.Warn(Tracing.NotCreatedEntityCommand(input.Target));
                return;
            }

            if (!CanMove(squad))
            {
                return;
            }

            ref var dir = ref _world.GetRef<Direction>(squad);
            ref var rot = ref _world.GetRef<Rotation>(squad);
            ref var fix = ref _world.GetRef<Fixation>(squad);
            dir = input.Direction;
            fix = input.Fixation;

            if (fix2.SqrLength(dir) == 0)
            {
                return;
            }

            rot = Maths.Degrees(MathsExtensions.GetRotation(dir));
        }

        private bool CanMove(Entity squad)
        {
            var squadId = _world.Get<SyncId>(squad);
            (SyncId SquadId, int Count) state = (squadId, 0);
            var query = _desc;
            _world.ForEach<(SyncId SquadId, int Count), SquadMember>(in query, ref state, static (ref (SyncId SquadId, int Count) context, in SquadMember member) =>
            {
                if (member.SquadId == context.SquadId)
                {
                    context.Count++;
                }
            }).Invoke(ref state);
            return state.Count > 0;
        }
    }
}
