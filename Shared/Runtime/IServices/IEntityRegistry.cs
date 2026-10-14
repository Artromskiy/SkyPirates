using Delta.ECS;
using DVG.Components;

namespace DVG.SkyPirates.Shared.IServices
{
    public interface IEntityRegistry
    {
        int NextId { get; }
        void Reserve(SyncId syncId);
        void Register(Entity entity, SyncId syncId);
        void Reserve(SyncIdReserve syncIdReserve);
        SyncId Reserve();
        SyncIdReserve Reserve(int count);
        bool TryGet(SyncId syncId, out Entity entity);
    }
}
