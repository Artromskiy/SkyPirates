using Arch.Core;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.ViewModels;

namespace DVG.SkyPirates.Client.Factories.VMFactories
{
    public class EntityVMFactory : IEntityVMFactory
    {
        public IEntityVM Create((World world, Entity entity) parameters) =>
            new EntityVM(parameters.world, parameters.entity);
    }
}
