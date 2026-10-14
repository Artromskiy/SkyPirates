using Delta.ECS;
using DVG.Components.Attributes;
using System.Collections.Generic;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("Targets")]
    [ComponentGroup(Group.Temporary)]
    [Component(false)]
    public struct Targets
    {
        public List<Entity> Entities;
    }
}
