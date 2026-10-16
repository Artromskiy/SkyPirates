using Delta.Netcode;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.IServices;

namespace DVG.SkyPirates.Shared.Services.CommandExecutors
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
