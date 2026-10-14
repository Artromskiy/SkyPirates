using Delta.ECS;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.IFactories;
using SkyPirates.Shared.IServices;
using System;

namespace SkyPirates.Shared.Factories
{
	public class EntityFactory : IEntityFactory
	{
		private readonly World _world;
		private readonly IEntityRegistry _entityRegistryService;

		public EntityFactory(World world, IEntityRegistry entityRegistryService)
		{
			_world = world;
			_entityRegistryService = entityRegistryService;
		}

		public Entity Create(EntityParameters parameters)
		{
			if (!_entityRegistryService.TryGet(parameters.SyncId, out var entity) ||
				!entity.IsValid || !_world.IsAlive(entity))
			{
				entity = _world.Create<Alive>();
				_entityRegistryService.Register(entity, parameters.SyncId);
			}
			_world.Add(entity, parameters.SyncId, parameters.SyncIdReserve, parameters.RandomSeed, new Alive());
			return entity;
		}
	}
}
