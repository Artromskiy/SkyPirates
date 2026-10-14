using DVG.SkyPirates.Server.IServices;
using DVG.SkyPirates.Shared.Ids;

namespace DVG.SkyPirates.Server.Services
{
    internal class CheatLoggerService : ICheatLoggerService
    {
        public bool AssertCheating(bool isCheating, int clientId, CheatingId cheatingId)
        {
            if (isCheating)
                LogCheat(clientId, cheatingId);
            return isCheating;
        }

        public void LogCheat(int clientId, CheatingId cheatingId)
        {
            //Debug.Assert(false);
        }
    }
}
