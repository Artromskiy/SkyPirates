using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Client.IServices
{
    public interface IClientService : ITickableExecutor
    {
        bool IsConnected { get; }
        int Id { get; }
    }
}
