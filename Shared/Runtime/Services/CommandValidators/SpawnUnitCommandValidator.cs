using Delta.ECS;
using Delta.Netcode;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IServices;

namespace SlyPirates.Shared.Services.CommandValidators
{
    public sealed class SpawnUnitCommandValidator : ICommandValidator<SpawnUnitCommand>, ICommandValidatorRegistration
    {
        private readonly World _world;
        private readonly IEntityRegistry _entities;

        public SpawnUnitCommandValidator(World world, IEntityRegistry entities)
        {
            _world = world;
            _entities = entities;
        }

        public bool Validate(in Command<SpawnUnitCommand> command, in CommandValidationContext context)
        {
            if (!_entities.TryGet(command.Payload.SquadId, out Entity squad)
                || !_world.IsAlive(squad)
                || !_world.Has<Alive>(squad)
                || !_world.Has<Squad>(squad))
            {
                return false;
            }

            return _world.Get<Squad>(squad).Deck.Contains(command.Payload.UnitId);
        }
    }
}
