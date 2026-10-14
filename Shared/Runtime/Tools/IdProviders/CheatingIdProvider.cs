using SkyPirates.Shared.Core.Tools;
using SkyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SkyPirates.Shared.Tools.IdProviders
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
