using Delta.Netcode;
using SlyPirates.Shared.IServices;

namespace SlyPirates.Shared.Services.CommandValidators
{
    public sealed class ZeroTickCommandValidator<T> : ICommandValidator<T>, ICommandValidatorRegistration
    {
        public bool Validate(in Command<T> command, in CommandValidationContext context)
        {
            return command.Header.Step > 0;
        }
    }
}
