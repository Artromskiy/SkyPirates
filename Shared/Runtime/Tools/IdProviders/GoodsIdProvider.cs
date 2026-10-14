using DVG.Core.Tools;
using DVG.SkyPirates.Shared.Ids;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Tools.IdProviders
{
    public class GoodsIdProvider : StringIdProvider<GoodsIdProvider, GoodsId>
    {
        public override IEnumerable<GoodsId> TypedIds => new GoodsId[]
        {

        };
    }
}
