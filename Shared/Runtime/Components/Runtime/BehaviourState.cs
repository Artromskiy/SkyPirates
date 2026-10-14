using Delta.ECS;
using Delta;
using DVG.Components.Attributes;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema]
    [ComponentAlias("BehaviourState")]
    [ComponentGroup(Group.History)]
    [Component(true)]
    public struct BehaviourState
    {
        public StateId State;
        public fix Percent;
        public fix Duration;

        public StateId? ForceState;
    }
}
