using Delta.Netcode;
using DVG.SkyPirates.Shared.Core;
using DVG.SkyPirates.Shared;
using DVG.SkyPirates.Shared.IServices;

namespace DVG.SkyPirates.Shared.Services.CommandValidators
{
    public sealed class LateCommandValidator<T> : ICommandValidator<T>, ICommandValidatorRegistration
    {
        public bool Validate(in Command<T> command, in CommandValidationContext context)
        {
            return command.Header.Step > context.CurrentStep - Constants.ValidTicksCount;
        }
    }
}
