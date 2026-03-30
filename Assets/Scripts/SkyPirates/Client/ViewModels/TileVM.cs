using DVG.Core;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Ids;

namespace DVG.SkyPirates.Client.ViewModels
{
    public class TileVM : ITileVM
    {
        public TileId TileId { get; }
        public float3 Position { get; }
        public int3 AxialPosition { get; }

        public TileVM(HexMap hexMap, int3 axialPosition)
        {
            TileId = hexMap.Data[axialPosition];
            AxialPosition = axialPosition;
            Position = (float3)Hex.AxialToWorld(axialPosition);
        }
    }
}
