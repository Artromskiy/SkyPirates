using Delta.ECS;
using Delta;
using SlyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SlyPirates.Shared.Components.Config
{
    [ComponentSchema(0xD6E5CBE207A5169BUL)]
    [ComponentAlias("HexMap")]
    public struct HexMap
    {
        public Dictionary<int3, TileId> Data;
    }
}
