using SkyPirates.Shared.IServices.TickableExecutors;

namespace SkyPirates.Client.IServices
{
    public interface IClientService : ITickableExecutor
    {
        bool IsConnected { get; }
        int Id { get; }
    }
}
