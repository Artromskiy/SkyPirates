using SlyPirates.Shared.Core.Tools;
using SlyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SlyPirates.Shared.Tools.IdProviders
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
