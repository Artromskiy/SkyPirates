using Delta.Netcode;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Shared.Services.CommandExecutors
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
