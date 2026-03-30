using DVG.Commands;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Client.IServices
{
    public interface ICommandSendScheduler : ITickableExecutor
    {
        void SendCommand<T>(Command<T> data);
    }
}
