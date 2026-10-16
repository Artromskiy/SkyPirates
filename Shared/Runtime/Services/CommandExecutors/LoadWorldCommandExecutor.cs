using Delta.Netcode;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IServices;

namespace SlyPirates.Shared.Services.CommandExecutors
{
    public class LoadWorldCommandExecutor : ICommandExecutorRegistration, ICommandExecutor<LoadWorldCommand>
    {
        private readonly IWorldHistory _history;

        public LoadWorldCommandExecutor(IWorldHistory history)
        {
            _history = history;
        }

        public void Execute(in Command<LoadWorldCommand> cmd)
        {
            _history.ApplySnapshot(cmd.Payload.WorldData, checked((int)cmd.Header.Step - 1));
        }
    }
}
