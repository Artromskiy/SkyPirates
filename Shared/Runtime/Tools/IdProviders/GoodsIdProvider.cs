using SlyPirates.Shared.Core.Tools;
using SlyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SlyPirates.Shared.Tools.IdProviders
{
    public class GoodsIdProvider : StringIdProvider<GoodsIdProvider, GoodsId>
    {
        public override IEnumerable<GoodsId> TypedIds => new GoodsId[]
        {

        };
    }
}
