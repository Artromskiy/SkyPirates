using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.IServices;

namespace DVG.SkyPirates.Shared.Factories
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
            _entityDependencyService.Apply(entity);
            _componentDefaultsService.Apply(entity);
            _world.GetRef<TeamId>(entity) = parameters.team;
            return entity;
        }
    }
}
