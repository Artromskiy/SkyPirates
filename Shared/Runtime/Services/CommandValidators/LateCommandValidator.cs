using Delta.Netcode;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IServices;

namespace SlyPirates.Shared.Services.CommandValidators
{
    public sealed class LateCommandValidator<T> : ICommandValidator<T>, ICommandValidatorRegistration
    {
        private readonly IWorldHistory _history;
        public LateCommandValidator(IWorldHistory history) => _history = history;

        public bool Validate(in Command<T> command, in CommandValidationContext context)
        {
            return command.Header.Step > _history.OldestTick;
        }
    }
}
