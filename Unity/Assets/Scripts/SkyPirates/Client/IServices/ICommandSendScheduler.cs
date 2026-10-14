using SkyPirates.Shared.IServices.TickableExecutors;

namespace SkyPirates.Client.IServices
{
    public interface ICommandSendScheduler : ITickableExecutor
    {
        void SendCommand<T>(T data);
        void SetTransientInput<T>(T data);
        bool TryGetTransientInput<T>(out T data);
    }
}
