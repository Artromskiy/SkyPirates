using Delta.ECS;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.ViewModels;

namespace DVG.SkyPirates.Client.Factories.VMFactories
{
    public class CachedEntityVMFactory : IEntityVMFactory
    {
        public IEntityVM Create((World world, Entity entity) parameters) =>
            new CachedEntityVM(parameters.world, parameters.entity);
    }
}
