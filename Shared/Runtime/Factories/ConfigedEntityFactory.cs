using Delta.ECS;
using DVG.SkyPirates.Shared.Core.Ids;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.IServices;
using System;

namespace DVG.SkyPirates.Shared.Factories
{
    public class ConfigedEntityFactory<T> : IConfigedEntityFactory<T>
        where T : struct, IId, IEquatable<T>
    {
        private readonly IEntityFactory _entityFactory;
        private readonly IEntityConfigFactory<T> _entityConfigFactory;
        private readonly IComponentDependenciesService _entityDependencyService;
        private readonly IComponentDefaultsService _componentDefaultsService;
        private readonly World _world;

        public ConfigedEntityFactory(IEntityFactory entityFactory, IEntityConfigFactory<T> entityConfigFactory, IComponentDependenciesService entityDependencyService, IComponentDefaultsService componentDefaultsService, World world)
        {
            _entityFactory = entityFactory;
            _entityConfigFactory = entityConfigFactory;
            _entityDependencyService = entityDependencyService;
            _componentDefaultsService = componentDefaultsService;
            _world = world;
        }

        public Entity Create((T Id, EntityParameters parameters) parameters)
        {
            var config = _entityConfigFactory.Create(parameters.Id);
            var entity = _entityFactory.Create(parameters.parameters);
            _world.SetEntityData(entity, config);
            _entityDependencyService.ProcessChanges();
            _componentDefaultsService.ProcessChanges();
            return entity;
        }
    }
}
