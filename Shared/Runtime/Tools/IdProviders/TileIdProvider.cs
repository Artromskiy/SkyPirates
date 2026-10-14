using SkyPirates.Shared.Core.Tools;
using SkyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SkyPirates.Shared.Tools.IdProviders
{
    public class TileIdProvider : StringIdProvider<TileIdProvider, TileId>
    {
        public override IEnumerable<TileId> TypedIds => new TileId[]
        {
            new("Land1"),
            new("Land2"),
            new("Land3"),
            new("Land4"),
            new("Land5"),
            new("Air"),
        };
    }
}
