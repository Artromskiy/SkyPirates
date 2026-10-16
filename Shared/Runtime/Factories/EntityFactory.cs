using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.IServices;
using System;

namespace DVG.SkyPirates.Shared.Factories
{
	public class EntityFactory : IEntityFactory
	{
		private readonly World _world;
		private readonly IEntityRegistry _entityRegistryService;
		private readonly EntityIdMap _entities;

		public EntityFactory(World world, IEntityRegistry entityRegistryService)
		{
			_world = world;
			_entityRegistryService = entityRegistryService;
			_entities = world.GetEntityIdMap();
		}

		public Entity Create(EntityParameters parameters)
		{
			if (!_entities.TryGetRetainedEntity(parameters.SyncId, out var entity) ||
				!entity.IsValid || !_world.IsAlive(entity))
			{
				entity = _world.Create<Alive>();
			}
			_world.Add(entity, parameters.SyncId, parameters.SyncIdReserve, parameters.RandomSeed, new Alive());
			_entityRegistryService.Register(entity, parameters.SyncId);
			return entity;
		}
	}
}
