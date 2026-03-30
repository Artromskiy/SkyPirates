using Arch.Core;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.ViewModels;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Client.Factories.VMFactories
{
    public class CachedEntityVMFactory : IEntityVMFactory
    {
        private readonly ITickCounterService _tickCounterService;

        public CachedEntityVMFactory(ITickCounterService tickCounterService)
        {
            _tickCounterService = tickCounterService;
        }

        public IEntityVM Create((World world, Entity entity) parameters) =>
            new CachedEntityVM(_tickCounterService, parameters.world, parameters.entity);
    }
}
