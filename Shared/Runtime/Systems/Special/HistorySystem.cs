using Delta.ECS;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Shared.Systems.Special
{
    public sealed class HistorySystem : IHistorySystem
    {
        private readonly IWorldHistory _history;
        private readonly SnapshotHistorySystem _snapshot;
        private readonly IEntityRegistry _entityRegistry;

        public HistorySystem(
            World world,
            IWorldHistory history,
            IEntityFactory entityFactory,
            IEntityRegistry entityRegistry,
            IComponentDependenciesService componentDependencies)
        {
            _history = history;
            _snapshot = new SnapshotHistorySystem(world, history, entityFactory, entityRegistry, componentDependencies);
            _entityRegistry = entityRegistry;
        }

        public void GoTo(int tick)
        {
            _history.GoTo(tick);
            _entityRegistry.Synchronize();
        }

        public void Rollback(int tick)
        {
            _history.Rollback(tick);
            _entityRegistry.Synchronize();
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
            _entityRegistry.Rebuild();
        }

        public WorldData GetSnapshot(int tick)
        {
            return _history.GetSnapshot(tick);
        }
    }
}
