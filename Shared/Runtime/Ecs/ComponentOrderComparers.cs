using Delta.ECS;
using DVG.Components;
using DVG.SkyPirates.Shared.Components.Runtime;

namespace DVG.SkyPirates.Shared.Ecs
{
    internal struct SquadIdComparer : IComponentComparer
    {
        public int Invoke(in SquadMember left, in SquadMember right) =>
            left.SquadId.CompareTo(right.SquadId);
    }

    internal struct SyncIdComparer : IComponentComparer
    {
        public int Invoke(in SyncId left, in SyncId right) =>
            left.Value.CompareTo(right.Value);
    }
}
