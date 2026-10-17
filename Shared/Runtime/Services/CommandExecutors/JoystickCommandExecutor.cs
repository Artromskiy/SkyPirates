using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using Delta.Netcode;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.Systems;
using SlyPirates.Shared.Tools.Extensions;
using SlyPirates.Shared.Tools.TraceHelpers;
using System.Diagnostics;
using System;

namespace SlyPirates.Shared.Services.CommandExecutors
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

            Entity controlledEntity = squad;
            if (_world.Has<OnShip>(squad))
            {
                controlledEntity = _world.GetEntity(_world.Get<OnShip>(squad).ShipId);
            }
            else if (!CanMove(squad))
            {
                return;
            }

            ref var dir = ref _world.GetRef<Direction>(controlledEntity);
            ref var rot = ref _world.GetRef<Rotation>(controlledEntity);
            ref var fix = ref _world.GetRef<Fixation>(controlledEntity);
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
                if (member.SquadId.Value == context.SquadId.Value)
                {
                    context.Count++;
                }
            }).Invoke(ref state);
            return state.Count > 0;
        }
    }
}
