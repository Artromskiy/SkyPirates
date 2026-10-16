using System.Collections.Generic;

namespace SlyPirates.Shared.Data
{
    public sealed class ComponentDefaultsConfig : List<ComponentDefaultsData> { }

    public class ComponentDefaultsData
    {
        public ComponentDataSet Default = default!;
        public ComponentIdList CopyFrom = default!;
        public ComponentIdList CopyTo = default!;
    }
}
