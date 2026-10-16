using Delta.Netcode;
using SlyPirates.Shared.IServices;

namespace SlyPirates.Shared.Services.CommandValidators
{
    public sealed class FutureCommandValidator<T> : ICommandValidator<T>, ICommandValidatorRegistration
    {
        public bool Validate(in Command<T> command, in CommandValidationContext context)
        {
            return context.CurrentStep + 1 >= command.Header.Step;
        }
    }
}
