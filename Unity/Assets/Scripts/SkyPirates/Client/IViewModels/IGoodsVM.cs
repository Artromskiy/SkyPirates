using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SkyPirates.Client.IViewModels
{
    public interface IGoodsVM : IViewModel
    {
        IReadOnlyDictionary<GoodsId, int> Goods { get; }
    }
}
