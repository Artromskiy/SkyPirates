using DVG.SkyPirates.Shared.Data;

namespace DVG.SkyPirates.Shared.IServices.TickableExecutors
{
    public interface IHistorySystem
    {
        void Save(int tick);
        void SaveBaseline();
        void GoTo(int tick);
        void Rollback(int tick);
        WorldData GetSnapshot(int tick);
        void ApplySnapshot(WorldData snapshot);
    }
}
