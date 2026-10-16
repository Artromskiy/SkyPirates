using Delta.ECS;
using System.Collections.Generic;
using SlyPirates.Shared.Components.Runtime;

namespace SlyPirates.Shared.Components.Framed
{
    [ComponentSchema(0x9FD48E51741652F9UL)]
    [ComponentAlias("Targets")]
    [ComponentGroup(Group.Temporary)]
    public struct Targets
    {
        public List<SyncId> SyncIds;
    }
}
