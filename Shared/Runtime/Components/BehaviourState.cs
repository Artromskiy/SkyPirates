using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0xDE8F79EDE47888A4UL)]
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
