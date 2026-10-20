using Delta.ECS;

namespace SlyPirates.Shared.Components.Runtime
{
    [ComponentSchema(0xA48E69433DD55E0DUL)]
    [ComponentAlias("OnShip")]
    [ComponentGroup(Group.History)]
    public struct OnShip
    {
        public SyncId ShipId;
        public SyncId SquadId;
        public EntityData Components;
    }
}
