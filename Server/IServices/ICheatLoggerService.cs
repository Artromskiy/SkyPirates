using DVG.SkyPirates.Shared.Ids;

namespace DVG.SkyPirates.Server.IServices
{
    public interface ICheatLoggerService
    {
        void LogCheat(int clientId, CheatingId cheatingId);
        bool AssertCheating(bool isCheating, int clientId, CheatingId cheatingId);
    }
}
