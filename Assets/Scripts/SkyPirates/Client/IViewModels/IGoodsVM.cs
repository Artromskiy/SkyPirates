using DVG.Core;
using DVG.SkyPirates.Shared.Ids;
using System.Collections.Generic;

namespace DVG.SkyPirates.Client.IViewModels
{
    public interface IGoodsVM : IViewModel
    {
        IReadOnlyDictionary<GoodsId, int> Goods { get; }
    }
}
