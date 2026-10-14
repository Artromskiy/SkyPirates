using Delta.ECS;
using Delta;
using SkyPirates.Shared.Ids;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("BehaviourState")]
    [ComponentGroup(Group.History)]
    public struct BehaviourState
    {
        public StateId State;
        public fix Percent;
        public fix Duration;

        public StateId? ForceState;
    }
}
