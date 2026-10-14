using DVG.Core.Tools;
using DVG.SkyPirates.Shared.Ids;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Tools.IdProviders
{
    public class CheatingIdProvider : StringIdProvider<CheatingIdProvider, CheatingId>
    {
        public override IEnumerable<CheatingId> TypedIds => new CheatingId[]
        {
            new("NoOwnershipCommand"),
            new("FutureCommand"),
            new("WrongClientId")
        };
    }
}
