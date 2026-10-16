using SlyPirates.Server.IServices;
using SlyPirates.Shared.Ids;

namespace SlyPirates.Server.Services
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
