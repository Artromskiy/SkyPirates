using Delta.Netcode;
using SkyPirates.Shared.Commands;
using SkyPirates.Shared.IServices;
using SkyPirates.Shared.IServices.TickableExecutors;

namespace SkyPirates.Shared.Services.CommandExecutors
{
    public class LoadWorldCommandExecutor : ICommandExecutorRegistration, ICommandExecutor<LoadWorldCommand>
    {
        private readonly IHistorySystem _historySystem;

        public LoadWorldCommandExecutor(IHistorySystem historySystem)
        {
            _historySystem = historySystem;
        }

        public void Execute(in Command<LoadWorldCommand> cmd)
        {
            _historySystem.ApplySnapshot(cmd.Payload.WorldData);
            _historySystem.SaveBaseline();
        }
    }
}
