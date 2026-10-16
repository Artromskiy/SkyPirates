using SlyPirates.Shared.Core.Tools;
using SlyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SlyPirates.Shared.Tools.IdProviders
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
