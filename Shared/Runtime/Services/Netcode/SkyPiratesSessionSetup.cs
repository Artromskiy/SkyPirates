using Delta.Netcode;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;
using System.Linq;

namespace DVG.SkyPirates.Shared.Services.Netcode
{
    public sealed class SkyPiratesSessionSetup
    {
        private readonly IHistorySystem _history;
        private readonly IDeltaTickableService<IDeltaTickableExecutor> _systems;
        private readonly ITickableService<IInTickable> _inTickables;
        private readonly IDisposeSystem _disposeSystem;
        private readonly IEntityRegistry _entityRegistry;
        private readonly ICommandReciever _commandReciever;
        private readonly ICommandExecutorRegistration[] _executors;
        private readonly ICommandValidatorRegistration[] _validators;
        private readonly ICommandMutatorRegistration[] _mutators;

        public SkyPiratesSessionSetup(
            IHistorySystem history,
            IDeltaTickableService<IDeltaTickableExecutor> systems,
            ITickableService<IInTickable> inTickables,
            IDisposeSystem disposeSystem,
            IEntityRegistry entityRegistry,
            ICommandReciever commandReciever,
            IEnumerable<ICommandExecutorRegistration> executors,
            IEnumerable<ICommandValidatorRegistration> validators,
            IEnumerable<ICommandMutatorRegistration> mutators)
        {
            _history = history;
            _systems = systems;
            _inTickables = inTickables;
            _disposeSystem = disposeSystem;
            _entityRegistry = entityRegistry;
            _commandReciever = commandReciever;
            _executors = executors.ToArray();
            _validators = validators.ToArray();
            _mutators = mutators.ToArray();
        }

        public SessionHost Create(SessionMode mode, AuthorId authorId, ISessionTransport transport, long startStep = 0)
        {
            var start = new SessionStart(new SessionId(1), authorId, new ProtocolId(1), startStep, 0xA0761D6478BD642FUL);
            return Create(start, mode, transport);
        }

        public SessionHost Create(SessionStart start, SessionMode mode, ISessionTransport transport)
        {
            var commandRegistry = new CommandRegistry();
            foreach (ICommandRegistration registration in GeneratedCommands.Registrations)
            {
                commandRegistry.Register(registration);
            }

            var journal = new MemoryCommandJournal();
            var model = new SkyPiratesSessionModel(start.Step, _history, _systems, _inTickables, _disposeSystem);
            var host = new SessionHost(
                start,
                commandRegistry,
                new SkyPiratesCommandPayloadHandler(),
                model,
                journal,
                transport: transport,
                mode: mode,
                preparationState: new CommandPreparationState(checked((ulong)_entityRegistry.NextId), start.Seed));

            var policies = new SkyPiratesCommandPolicyRegistrar(
                host,
                mode,
                _executors,
                _validators,
                _mutators,
                _commandReciever);
            policies.Register();
            return host;
        }
    }
}
