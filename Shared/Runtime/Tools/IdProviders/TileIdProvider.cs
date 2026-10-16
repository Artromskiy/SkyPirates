using SlyPirates.Shared.Core.Tools;
using SlyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SlyPirates.Shared.Tools.IdProviders
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
