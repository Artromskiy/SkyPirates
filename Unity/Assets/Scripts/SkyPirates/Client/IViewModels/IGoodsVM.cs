using SlyPirates.Shared.Core;
using SlyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SlyPirates.Client.IViewModels
{
    public interface IGoodsVM : IViewModel
    {
        IReadOnlyDictionary<GoodsId, int> Goods { get; }
    }
}
