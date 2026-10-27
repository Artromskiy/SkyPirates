using Delta;
using Delta.ECS;

namespace SlyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0xBFE1EC02A3521D2DUL)]
    [ComponentAlias("Ship")]
    [ComponentGroup(Group.History)]
    public struct Ship
    {
        /// <summary>The SyncId of the squad assigned to this ship.</summary>
        public SyncId SquadId;
        public fix BoardingRadius;
        public fix DisembarkDistance;
        /// <summary>Seconds spent continuously inside BoardingRadius.</summary>
        public fix BoardingElapsedSeconds;
        /// <summary>Seconds remaining before another ship transition is allowed.</summary>
        public fix TransitionCooldownSeconds;
        /// <summary>Tracks island-range entry edges, including while transitions are on cooldown.</summary>
        public bool WasInsideDisembarkRange;
    }
}
