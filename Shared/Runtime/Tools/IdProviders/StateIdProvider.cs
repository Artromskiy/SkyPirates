using SlyPirates.Shared.Core.Tools;
using SlyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SlyPirates.Shared.Tools.IdProviders
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
