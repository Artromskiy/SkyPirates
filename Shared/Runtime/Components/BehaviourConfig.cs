using Delta.ECS;
using Delta;
using DVG.SkyPirates.Shared.Ids;
using System.Collections.Frozen;

namespace DVG.SkyPirates.Shared.Components.Config
{
    [ComponentSchema(0xFF349D915B97A18BUL)]
    [ComponentAlias("BehaviourConfig")]
    [ComponentGroup(Group.History)]
    public struct BehaviourConfig
    {
        public FrozenDictionary<StateId, StateId> Scenario;
        public FrozenDictionary<StateId, fix> Durations;
    }
}
