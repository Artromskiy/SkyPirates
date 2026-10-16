using Delta.Netcode;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IFactories;
using SlyPirates.Shared.IServices;

namespace SlyPirates.Shared.Services.CommandExecutors
{
    public class SpawnSquadCommandExecutor : ICommandExecutorRegistration, ICommandExecutor<SpawnSquadCommand>
    {
        private readonly ISquadFactory _squadFactory;

        public SpawnSquadCommandExecutor(
            ISquadFactory squadFactory)
        {
            _squadFactory = squadFactory;
        }

        public void Execute(in Command<SpawnSquadCommand> cmd)
        {
            TeamId team = (int)cmd.Header.Key.AuthorId.Value;
            var squad = _squadFactory.Create((cmd.Payload.CreationData, team));
        }
    }
}
