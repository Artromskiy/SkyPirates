using Delta.Netcode;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Commands;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.IServices;

namespace SkyPirates.Shared.Services.CommandMutators
{
    public sealed class SpawnCommandMutator :
        ICommandMutator<SpawnSquadCommand>,
        ICommandMutator<SpawnUnitCommand>,
        ICommandMutatorRegistration
    {
        private const uint ReservedIdCount = 64;
        private readonly IEntityRegistry _entityRegistry;

        public SpawnCommandMutator(IEntityRegistry entityRegistry)
        {
            _entityRegistry = entityRegistry;
        }

        public void Mutate(ref SpawnSquadCommand payload, CommandPreparation preparation)
        {
            payload.CreationData = CreateEntityParameters(preparation);
        }

        public void Mutate(ref SpawnUnitCommand payload, CommandPreparation preparation)
        {
            payload.CreationData = CreateEntityParameters(preparation);
        }

        private EntityParameters CreateEntityParameters(CommandPreparation preparation)
        {
            var syncId = new SyncId { Value = checked((int)preparation.NextId()) };
            var reserveFirst = checked((int)preparation.ReserveIds(ReservedIdCount));
            var syncIdReserve = new SyncIdReserve
            {
                First = reserveFirst,
                Count = (int)ReservedIdCount,
                Current = reserveFirst,
            };
            var randomSeed = new RandomSeed { Value = (int)preparation.NextSeed() };

            _entityRegistry.Reserve(syncId);
            _entityRegistry.Reserve(syncIdReserve);
            return new EntityParameters(syncId, syncIdReserve, randomSeed);
        }
    }
}
