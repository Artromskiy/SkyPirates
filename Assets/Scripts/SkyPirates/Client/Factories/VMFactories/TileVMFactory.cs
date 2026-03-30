using DVG.Core;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.ViewModels;
using DVG.SkyPirates.Shared.Components.Config;
namespace DVG.SkyPirates.Client.Factories.VMFactories
{
    public class TileVMFactory : IFactory<ITileVM, (HexMap map, int3 axialPosition)>
    {
        public ITileVM Create((HexMap map, int3 axialPosition) parameters) =>
            new TileVM(parameters.map, parameters.axialPosition);
    }
}
