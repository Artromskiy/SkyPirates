using Delta.Netcode;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.IServices;

namespace DVG.SkyPirates.Shared.Services.CommandValidators
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
