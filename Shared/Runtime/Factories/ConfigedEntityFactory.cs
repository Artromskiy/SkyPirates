using Delta.ECS;
using SkyPirates.Shared.Core.Ids;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.IFactories;
using SkyPirates.Shared.IServices;
using SkyPirates.Shared.Systems;
using System;

namespace SkyPirates.Shared.Factories
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
            _entityDependencyService.Apply(entity);
            _componentDefaultsService.Apply(entity);
            return entity;
        }
    }
}
