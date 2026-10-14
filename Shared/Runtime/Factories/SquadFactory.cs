using Delta.ECS;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.IFactories;
using SkyPirates.Shared.IServices;
using SkyPirates.Shared.Systems;

namespace SkyPirates.Shared.Factories
{
    public class SquadFactory : ISquadFactory
    {
        private readonly SquadStatsConfig _squadStats;
        private readonly IComponentDependenciesService _entityDependencyService;
        private readonly IComponentDefaultsService _componentDefaultsService;
        private readonly IEntityFactory _entityFactory;
        private readonly World _world;

        public SquadFactory(SquadStatsConfig squadStats, IComponentDependenciesService entityDependencyService, IComponentDefaultsService componentDefaultsService, IEntityFactory entityFactory, World world)
        {
            _squadStats = squadStats;
            _entityDependencyService = entityDependencyService;
            _componentDefaultsService = componentDefaultsService;
            _entityFactory = entityFactory;
            _world = world;
        }

        public Entity Create((EntityParameters entityParameters, TeamId team) parameters)
        {
            var entity = _entityFactory.Create(parameters.entityParameters);
            _world.Add<Squad>(entity);
            _world.SetEntityData(entity, _squadStats[0]);
            _entityDependencyService.ProcessChanges();
            _componentDefaultsService.ProcessChanges();
            _world.GetRef<TeamId>(entity) = parameters.team;
            return entity;
        }
    }
}
