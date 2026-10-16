using SlyPirates.Server.IServices;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Services.Netcode;

namespace SlyPirates.Server.Services
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
