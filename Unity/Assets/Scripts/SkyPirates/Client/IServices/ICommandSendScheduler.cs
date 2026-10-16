using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Client.IServices
{
    public interface ICommandSendScheduler : ITickableExecutor
    {
        void SendCommand<T>(T data);
        void SetTransientInput<T>(T data);
        bool TryGetTransientInput<T>(out T data);
    }
}
