using Delta.ECS;
using System.Collections.Generic;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("Targets")]
    [ComponentGroup(Group.Temporary)]
    public struct Targets
    {
        public List<Entity> Entities;
    }
}
