using SkyPirates.Shared.Core.Tools;
using SkyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SkyPirates.Shared.Tools.IdProviders
{
    public class UnitIdProvider : StringIdProvider<UnitIdProvider, UnitId>
    {
        public override IEnumerable<UnitId> TypedIds => new UnitId[]
        {
            new("Sailor"),
            new("Pirate"),
            new("Buccaneer"),
            new("Rogue"),
            new("Skelly"),
            new("Bonny"),
            new("Daemon"),
            new("Imp"),
            new("Militia"),
        };
    }
}
