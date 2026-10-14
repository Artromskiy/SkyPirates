using Delta;
using System.Collections.Generic;

namespace SkyPirates.Shared.Data
{
    public class PackedCirclesConfig : Dictionary<int, PackedCirclesData> { }

    public class PackedCirclesData
    {
        public fix Radius;
        public fix2[] Points = default!;
    }
}
