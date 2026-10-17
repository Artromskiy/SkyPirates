using Delta.ECS;
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
        private readonly World _world;

        public SpawnSquadCommandExecutor(
            ISquadFactory squadFactory,
            World world)
        {
            _squadFactory = squadFactory;
            _world = world;
        }

        public void Execute(in Command<SpawnSquadCommand> cmd)
        {
            TeamId team = (int)cmd.Header.Key.AuthorId.Value;
            var squad = _squadFactory.Create((cmd.Payload.CreationData, team));
            _world.GetRef<Squad>(squad).Deck = cmd.Payload.Deck;
        }
    }
}
