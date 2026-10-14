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
        private readonly World _world;
        private readonly IEntityFactory _entityFactory;
        private readonly IEntityRegistry _entityRegistry;
        private readonly IComponentDependenciesService _componentDependencies;

        public HistorySystem(
            World world,
            IWorldHistory history,
            IEntityFactory entityFactory,
            IEntityRegistry entityRegistry,
            IComponentDependenciesService componentDependencies)
        {
            _world = world;
            _history = history;
            _entityFactory = entityFactory;
            _entityRegistry = entityRegistry;
            _componentDependencies = componentDependencies;
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
            WorldSnapshotApplier.Apply(
                _world,
                _history,
                _entityFactory,
                _entityRegistry,
                _componentDependencies,
                snapshot);
        }

        public WorldData GetSnapshot(int tick)
        {
            return _history.GetSnapshot(tick);
        }
    }
}
