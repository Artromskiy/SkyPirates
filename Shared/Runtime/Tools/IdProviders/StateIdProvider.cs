using SkyPirates.Shared.Core.Tools;
using SkyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SkyPirates.Shared.Tools.IdProviders
{
    public class StateIdProvider : StringIdProvider<StateIdProvider, StateId>
    {
        public override IEnumerable<StateId> TypedIds => new StateId[]
        {
            new("None"),
            new("PreAttack"),
            new("PostAttack"),
            new("Reload"),
        };

    }
}
