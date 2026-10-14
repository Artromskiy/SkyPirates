using SkyPirates.Shared.Core.Tools;
using SkyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SkyPirates.Shared.Tools.IdProviders
{
    public class GoodsIdProvider : StringIdProvider<GoodsIdProvider, GoodsId>
    {
        public override IEnumerable<GoodsId> TypedIds => new GoodsId[]
        {

        };
    }
}
