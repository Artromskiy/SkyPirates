using Delta.ECS;
using DVG.SkyPirates.Shared.Components.Runtime;

namespace DVG.SkyPirates.Shared.IServices
{
    public interface IEntityRegistry
    {
        int NextId { get; }
        void Rebuild();
        void Synchronize();
        void Reserve(SyncId syncId);
        void Register(Entity entity, SyncId syncId);
        void Reserve(SyncIdReserve syncIdReserve);
        bool TryGet(SyncId syncId, out Entity entity);
    }
}
