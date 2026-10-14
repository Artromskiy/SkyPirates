using Delta.Netcode;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.IServices;

namespace DVG.SkyPirates.Shared.Services.CommandExecutors
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
