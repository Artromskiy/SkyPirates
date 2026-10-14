using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Client.IServices
{
    public interface IClientService : ITickableExecutor
    {
        bool IsConnected { get; }
        int Id { get; }
    }
}
