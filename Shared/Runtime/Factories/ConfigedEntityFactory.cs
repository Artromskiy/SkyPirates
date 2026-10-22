using Delta.ECS;
using SlyPirates.Shared.Core.Ids;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IFactories;
using SlyPirates.Shared.IServices;
using System;

namespace SlyPirates.Shared.Factories
{
    public class ConfigedEntityFactory<T> : IConfigedEntityFactory<T>
        where T : struct, IId, IEquatable<T>
    {
        private readonly IEntityFactory _entityFactory;
        private readonly IEntityConfigFactory _entityConfigFactory;
        private readonly IComponentDependenciesService _entityDependencyService;
        private readonly IComponentDefaultsService _componentDefaultsService;
        private readonly World _world;

        public ConfigedEntityFactory(IEntityFactory entityFactory, IEntityConfigFactory entityConfigFactory, IComponentDependenciesService entityDependencyService, IComponentDefaultsService componentDefaultsService, World world)
        {
            _entityFactory = entityFactory;
            _entityConfigFactory = entityConfigFactory;
            _entityDependencyService = entityDependencyService;
            _componentDefaultsService = componentDefaultsService;
            _world = world;
        }

        public Entity Create((T Id, EntityParameters parameters) parameters)
        {
            var config = _entityConfigFactory.Create(parameters.Id.Value);
            var entity = _entityFactory.Create(parameters.parameters, config.ComponentIds);
            _world.AddOrSet(entity, config);
            _entityDependencyService.Apply(entity);
            _componentDefaultsService.Apply(entity);
            return entity;
        }
    }
}
