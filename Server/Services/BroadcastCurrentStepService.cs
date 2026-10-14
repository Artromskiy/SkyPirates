using SkyPirates.Server.IServices;
using SkyPirates.Shared.IServices.TickableExecutors;
using SkyPirates.Shared.Services.Netcode;

namespace SkyPirates.Server.Services
{
    public class BroadcastCurrentStepService : ITickableExecutor
    {
        private readonly SkyPiratesSessionProvider _session;

        public BroadcastCurrentStepService(SkyPiratesSessionProvider session)
        {
            _session = session;
        }

        public void Tick(int tick)
        {
            _session.Server.BroadcastCurrentStep(_session.Session.Start.SessionId.Value);
        }
    }
}
