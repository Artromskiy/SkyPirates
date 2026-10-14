using Delta.Netcode;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.IServices;
using System.Linq;

namespace DVG.SkyPirates.Shared.Services.Netcode
{
    internal sealed class SkyPiratesCommandPolicyRegistrar
    {
        private readonly SessionHost _host;
        private readonly SessionMode _mode;
        private readonly ICommandExecutorRegistration[] _executors;
        private readonly ICommandValidatorRegistration[] _validators;
        private readonly ICommandMutatorRegistration[] _mutators;
        private readonly ICommandReciever _receiver;

        public SkyPiratesCommandPolicyRegistrar(
            SessionHost host,
            SessionMode mode,
            ICommandExecutorRegistration[] executors,
            ICommandValidatorRegistration[] validators,
            ICommandMutatorRegistration[] mutators,
            ICommandReciever receiver)
        {
            _host = host;
            _mode = mode;
            _executors = executors;
            _validators = validators;
            _mutators = mutators;
            _receiver = receiver;
        }

        public void Register()
        {
            var visitor = new RegisterPolicies(this);
            foreach (ICommandRegistration registration in GeneratedCommands.Registrations)
            {
                registration.Visit(ref visitor);
            }
        }

        private void Register<T>()
        {
            var validators = _validators.OfType<ICommandValidator<T>>().ToArray();
            if (validators.Length > 0)
            {
                _host.Register<T>(new CompositeValidator<T>(validators));
            }

            var mutators = _mutators.OfType<ICommandMutator<T>>().ToArray();
            if (mutators.Length > 0)
            {
                _host.Register<T>(new CompositeMutator<T>(mutators));
            }

            var executors = _executors.OfType<ICommandExecutor<T>>().ToArray();
            var receiver = _mode == SessionMode.Local ? _receiver : null;
            if (executors.Length > 0 || receiver != null)
            {
                _host.Register<T>(new CompositeExecutor<T>(executors, receiver));
            }
        }

        private readonly struct RegisterPolicies : ICommandVisitor
        {
            private readonly SkyPiratesCommandPolicyRegistrar _registrar;

            public RegisterPolicies(SkyPiratesCommandPolicyRegistrar registrar)
            {
                _registrar = registrar;
            }

            public void Visit<T>() => _registrar.Register<T>();
        }

        private sealed class CompositeValidator<T> : ICommandValidator<T>
        {
            private readonly ICommandValidator<T>[] _validators;

            public CompositeValidator(ICommandValidator<T>[] validators) => _validators = validators;

            public bool Validate(in Command<T> command, in CommandValidationContext context)
            {
                for (int index = 0; index < _validators.Length; index++)
                {
                    if (!_validators[index].Validate(in command, in context))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private sealed class CompositeMutator<T> : ICommandMutator<T>
        {
            private readonly ICommandMutator<T>[] _mutators;

            public CompositeMutator(ICommandMutator<T>[] mutators) => _mutators = mutators;

            public void Mutate(ref T payload, CommandPreparation preparation)
            {
                for (int index = 0; index < _mutators.Length; index++)
                {
                    _mutators[index].Mutate(ref payload, preparation);
                }
            }
        }

        private sealed class CompositeExecutor<T> : ICommandExecutor<T>
        {
            private readonly ICommandExecutor<T>[] _executors;
            private readonly ICommandReciever _receiver;

            public CompositeExecutor(ICommandExecutor<T>[] executors, ICommandReciever receiver)
            {
                _executors = executors;
                _receiver = receiver;
            }

            public void Execute(in Command<T> command)
            {
                for (int index = 0; index < _executors.Length; index++)
                {
                    _executors[index].Execute(in command);
                }

                _receiver?.InvokeCommand(command);
            }
        }
    }
}
