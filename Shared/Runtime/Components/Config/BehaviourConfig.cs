using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Ids;
using System.Collections.Frozen;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema]
    [ComponentAlias("BehaviourConfig")]
    [ComponentGroup(Group.History)]
    public struct BehaviourConfig
    {
        public FrozenDictionary<StateId, StateId> Scenario;
        public FrozenDictionary<StateId, fix> Durations;
    }
}
