using Delta.ECS;
using Delta;
using SkyPirates.Shared.Ids;
using System.Collections.Frozen;
using SkyPirates.Shared.Components;

namespace SkyPirates.Shared.Components.Config
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
