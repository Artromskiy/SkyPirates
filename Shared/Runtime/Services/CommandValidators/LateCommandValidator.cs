using Delta.Netcode;
using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Shared.IServices;

namespace SkyPirates.Shared.Services.CommandValidators
{
    public sealed class LateCommandValidator<T> : ICommandValidator<T>, ICommandValidatorRegistration
    {
        public bool Validate(in Command<T> command, in CommandValidationContext context)
        {
            return command.Header.Step > context.CurrentStep - Constants.ValidTicksCount;
        }
    }
}
