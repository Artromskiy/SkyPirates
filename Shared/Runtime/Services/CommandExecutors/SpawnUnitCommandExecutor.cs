using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using Delta.Netcode;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.IFactories;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.Tools.TraceHelpers;
using System.Collections.Immutable;

namespace SlyPirates.Shared.Services.CommandExecutors
{
    public class SpawnUnitCommandExecutor : ICommandExecutorRegistration, ICommandExecutor<SpawnUnitCommand>
    {
        private readonly UnitsInfoConfig _unitsInfoConfig;
        private readonly IEntityRegistry _entityRegistryService;
        private readonly IConfigedEntityFactory<UnitId> _unitFactory;
        private readonly World _world;

        private static readonly GoodsId _rum = "Rum";

        public SpawnUnitCommandExecutor(UnitsInfoConfig unitsInfoConfig, IEntityRegistry entityRegistryService,
            IConfigedEntityFactory<UnitId> unitFactory, World world)
        {
            _unitsInfoConfig = unitsInfoConfig;
            _entityRegistryService = entityRegistryService;
            _unitFactory = unitFactory;
            _world = world;
        }

        public void Execute(in Command<SpawnUnitCommand> cmd)
        {
            if (!_entityRegistryService.TryGet(cmd.Payload.SquadId, out var squad))
            {
                Delta.Diagnostics.Trace.Warn(Tracing.NotCreatedEntityCommand(cmd.Payload.SquadId));
                return;
            }

            if (squad == default ||
                !_world.IsAlive(squad) ||
                !_world.Has<Alive>(squad))
            {
                Delta.Diagnostics.Trace.Warn(Tracing.NotCreatedEntityCommand(cmd.Payload.SquadId));
                return;
            }

            if (!TrySpawn(squad, cmd.Payload.UnitId))
            {
                return;
            }

            var pos = _world.Get<Position>(squad);
            var unit = _unitFactory.Create((cmd.Payload.UnitId, cmd.Payload.CreationData));

            _world.GetRef<TeamId>(unit) = (int)cmd.Header.Key.AuthorId.Value;
            _world.GetRef<Position>(unit) = pos;
            _world.GetRef<GoodsDrop>(unit) = new() { Values = ImmutableSortedDictionary.Create<GoodsId, int>() };
            _world.AddOrSet(unit, new SquadMember { SquadId = _world.Get<SyncId>(squad) });
        }


        private bool TrySpawn(Entity squad, UnitId unitId)
        {
            if (!_unitsInfoConfig.TryGetValue(unitId, out var info))
            {
                return false;
            }

            int price = info.Cost;
            if (price == 0)
            {
                return true;
            }

            var drop = _world.Get<GoodsDrop>(squad);
            int rum = drop.Values.GetValueOrDefault(_rum);
            if (rum < price)
            {
                return false;
            }

            ref var squadDrop = ref _world.GetRef<GoodsDrop>(squad);
            var newDrop = squadDrop.Values.ToBuilder();
            newDrop[_rum] = rum - price;
            squadDrop = new() { Values = newDrop.ToImmutable() };
            return true;
        }
    }
}
