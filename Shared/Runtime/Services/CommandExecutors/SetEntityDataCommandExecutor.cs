using Delta.ECS;
using Delta.Netcode;
using DVG.Components;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Systems;
using DVG.SkyPirates.Shared.Tools.TraceHelpers;
using System.Diagnostics;

namespace DVG.SkyPirates.Shared.Services.CommandExecutors
{
    public class SetEntityDataCommandExecutor : ICommandExecutorRegistration, ICommandExecutor<SetEntityDataCommand>
    {
        private readonly IEntityRegistry _entityRegistryService;
        private readonly World _world;

        public SetEntityDataCommandExecutor(IEntityRegistry entityRegistryService, World world)
        {
            _entityRegistryService = entityRegistryService;
            _world = world;
        }

        public void Execute(in Command<SetEntityDataCommand> cmd)
        {
            if (!_entityRegistryService.TryGet(cmd.Payload.Target, out var entity))
            {
                Delta.Diagnostics.Trace.Warn(Tracing.NotCreatedEntityCommand(cmd.Payload.Target));
                return;
            }
            var removeAction = new RemoveAllComponentsAction(_world, entity);
            ComponentsRegistry.ForEachData(ref removeAction);
            _world.SetEntityData(entity, cmd.Payload.Components);
        }

        private readonly struct RemoveAllComponentsAction : IStructGenericAction
        {
            private readonly World _world;
            private readonly Entity _entity;

            public RemoveAllComponentsAction(World world, Entity entity)
            {
                _world = world;
                _entity = entity;
            }

            public readonly void Invoke<T>() where T : struct
            {
                _world.Remove<T>(_entity);
            }
        }
    }
}
