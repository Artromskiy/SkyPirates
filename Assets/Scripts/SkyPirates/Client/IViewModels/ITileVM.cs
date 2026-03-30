using DVG.Core;
using DVG.SkyPirates.Shared.Ids;

namespace DVG.SkyPirates.Client.IViewModels
{
    public interface ITileVM : IViewModel
    {
        float3 Position { get; }
        int3 AxialPosition { get; }
        TileId TileId { get; }
    }
}
