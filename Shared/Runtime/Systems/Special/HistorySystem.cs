using Delta.ECS;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.IFactories;
using SkyPirates.Shared.IServices;
using SkyPirates.Shared.IServices.TickableExecutors;

namespace SkyPirates.Shared.Systems.Special
{
    public sealed class HistorySystem : IHistorySystem
    {
        private readonly IWorldHistory _history;
        private readonly SnapshotHistorySystem _snapshot;

        public HistorySystem(
            World world,
            IWorldHistory history,
            IEntityFactory entityFactory,
            IEntityRegistry entityRegistry,
            IComponentDependenciesService componentDependencies)
        {
            _history = history;
            _snapshot = new SnapshotHistorySystem(world, history, entityFactory, entityRegistry, componentDependencies);
        }

        public void GoTo(int tick)
        {
            _history.GoTo(tick);
        }

        public void Rollback(int tick)
        {
            _history.Rollback(tick);
        }

        public void Save(int tick)
        {
            _history.Save(tick);
        }

        public void SaveBaseline()
        {
            _history.SaveBaseline();
        }

        public void ApplySnapshot(WorldData snapshot)
        {
            _history.Clear();
            _snapshot.ApplySnapshot(snapshot);
        }

        public WorldData GetSnapshot(int tick)
        {
            return _history.GetSnapshot(tick);
        }
    }
}
