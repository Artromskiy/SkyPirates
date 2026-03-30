using Arch.Core;
using DVG.Core;
using DVG.SkyPirates.Client.IViewModels;

namespace DVG.SkyPirates.Client.IFactories
{
    public interface IEntityVMFactory : IFactory<IEntityVM, (World world, Entity entity)> { }
}