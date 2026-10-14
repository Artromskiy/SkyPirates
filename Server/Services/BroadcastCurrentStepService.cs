using DVG.SkyPirates.Server.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Services.Netcode;

namespace DVG.SkyPirates.Server.Services
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
