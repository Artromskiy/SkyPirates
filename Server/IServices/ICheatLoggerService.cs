using SkyPirates.Shared.Ids;

namespace SkyPirates.Server.IServices
{
    public interface ICheatLoggerService
    {
        void LogCheat(int clientId, CheatingId cheatingId);
        bool AssertCheating(bool isCheating, int clientId, CheatingId cheatingId);
    }
}
