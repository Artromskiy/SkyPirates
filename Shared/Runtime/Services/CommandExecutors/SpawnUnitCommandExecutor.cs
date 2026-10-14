using Delta;
using Delta.ECS;
using SkyPirates.Shared.Ecs;
using Delta.Netcode;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Commands;
using SkyPirates.Shared.Components.Config;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.Ids;
using SkyPirates.Shared.IFactories;
using SkyPirates.Shared.IServices;
using SkyPirates.Shared.Systems;
using SkyPirates.Shared.Tools.TraceHelpers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System;

namespace SkyPirates.Shared.Services.CommandExecutors
{
    public class SpawnUnitCommandExecutor : ICommandExecutorRegistration, ICommandExecutor<SpawnUnitCommand>
    {
        private readonly UnitsInfoConfig _unitsInfoConfig;
        private readonly IEntityRegistry _entityRegistryService;
        private readonly IConfigedEntityFactory<UnitId> _unitFactory;
        private readonly World _world;

        private static readonly GoodsId _rum = "Rum";
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.WhereAll<SquadMember, GoodsDrop, SyncId>().NotDisabled().Alive();

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
            _world.GetOrAdd<SquadMember>(unit).SquadId = _world.Get<SyncId>(squad).Value;
        }


        private bool TrySpawn(Entity squad, UnitId unitId)
        {
            // collect squad goods info
            // not enough => return
            // remove goods (first from squad then units sorted by syncId)
            // add new unit
            // end => redistribution system will do the work

            var squadId = _world.Get<SyncId>(squad);
            if (!_unitsInfoConfig.TryGetValue(unitId, out var info))
            {
                return false;
            }

            // TEMP: free summons while collecting history benchmark data.
            int price = 0;
            if (price == 0)
            {
                return true;
            }

            ref var drop = ref _world.GetRef<GoodsDrop>(squad);
            int squadRum = drop.Values.GetValueOrDefault(_rum);
            if (squadRum >= price)
            {
                var newDrop = drop.Values.ToBuilder();
                newDrop[_rum] -= price;
                drop = new() { Values = newDrop.ToImmutable() };
                return true;
            }

            (SyncId SquadId, int TotalRum) totalRumState = (squadId, squadRum);
            var query = _desc;
            _world.ForEach<(SyncId SquadId, int TotalRum), SquadMember, GoodsDrop>(in query, ref totalRumState,
                static (ref (SyncId SquadId, int TotalRum) context, in SquadMember member, in GoodsDrop goodsDrop) =>
                {
                    if (member.SquadId == context.SquadId)
                    {
                        context.TotalRum += goodsDrop.Values.GetValueOrDefault(_rum);
                    }
                }).Invoke(ref totalRumState);
            int totalRum = totalRumState.TotalRum;

            if (totalRum < price)
            {
                return false;
            }

            List<(Entity entity, GoodsDrop drop, SyncId syncId)> units = new();
            (SyncId SquadId, List<(Entity entity, GoodsDrop drop, SyncId syncId)> Units)
                collectState = (squadId, units);
            _world.ForEachEntity(in query, ref collectState,
                static (ref (SyncId SquadId, List<(Entity entity, GoodsDrop drop, SyncId syncId)> Units) context,
                    EntityRef entityRef, in SquadMember member, in GoodsDrop goodsDrop, in SyncId syncId) =>
                {
                    if (member.SquadId != context.SquadId)
                    {
                        return;
                    }

                    Entity entity = entityRef;
                    context.Units.Add((entity, goodsDrop, syncId));
                }).Invoke(ref collectState);

            int leftPrice = price;

            // remove from squad
            var squadNewDrop = drop.Values.ToBuilder();
            int removeSquad = Maths.Min(squadRum, price);
            if (squadRum > 0)
            {
                squadNewDrop[_rum] -= removeSquad;
                leftPrice -= removeSquad;
                drop = new() { Values = squadNewDrop.ToImmutable() };
            }

            // remove from units
            foreach (var unit in units.OrderBy(u => u.syncId.Value))
            {
                int count = unit.drop.Values.GetValueOrDefault(_rum);
                int remove = Maths.Min(count, leftPrice);
                if (remove == 0)
                {
                    continue;
                }

                var newDrop = unit.drop.Values.ToBuilder();
                newDrop[_rum] -= remove;
                leftPrice -= remove;
                _world.GetRef<GoodsDrop>(unit.entity) = new() { Values = newDrop.ToImmutable() };
                if (leftPrice == 0)
                {
                    break;
                }
            }

            return true;
        }
    }
}
