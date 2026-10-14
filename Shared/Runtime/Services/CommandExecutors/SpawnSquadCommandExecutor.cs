using Delta.Netcode;
using SkyPirates.Shared.Commands;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.IFactories;
using SkyPirates.Shared.IServices;

namespace SkyPirates.Shared.Services.CommandExecutors
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
