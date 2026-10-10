using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Client.IServices
{
    public interface ICommandSendScheduler : ITickableExecutor
    {
        void SendCommand<T>(T data);
        void SetTransientInput<T>(T data);
        bool TryGetTransientInput<T>(out T data);
    }
}
