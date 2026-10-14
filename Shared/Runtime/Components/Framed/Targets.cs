using Delta.ECS;
using System.Collections.Generic;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Framed
{
    [ComponentSchema]
    [ComponentAlias("Targets")]
    [ComponentGroup(Group.Temporary)]
    public struct Targets
    {
        public List<Entity> Entities;
    }
}
