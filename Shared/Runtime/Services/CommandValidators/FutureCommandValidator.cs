using Delta.Netcode;
using DVG.SkyPirates.Shared.IServices;

namespace DVG.SkyPirates.Shared.Services.CommandValidators
{
    public sealed class FutureCommandValidator<T> : ICommandValidator<T>, ICommandValidatorRegistration
    {
        public bool Validate(in Command<T> command, in CommandValidationContext context)
        {
            return context.CurrentStep + 1 >= command.Header.Step;
        }
    }
}
