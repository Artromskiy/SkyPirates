using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IFactories;
using SlyPirates.Shared.IServices;
using System;

namespace SlyPirates.Shared.Factories
{
    public class EntityFactory : IEntityFactory
    {
        private readonly World _world;
        private readonly IEntityRegistry _entityRegistryService;
        private readonly EntityIdMap _entities;
        private readonly ComponentId[] _components;

        public EntityFactory(World world, IEntityRegistry entityRegistryService)
        {
            _world = world;
            _entityRegistryService = entityRegistryService;
            _entities = world.GetEntityIdMap();
            _components = new[]
            {
                world.Components.GetPrimary<SyncId>(),
                world.Components.GetPrimary<SyncIdReserve>(),
                world.Components.GetPrimary<RandomSeed>(),
                world.Components.GetPrimary<Alive>()
            };
        }

        public Entity Create(EntityParameters parameters) => Create(parameters, ReadOnlySpan<ComponentId>.Empty);

        public Entity Create(EntityParameters parameters, ReadOnlySpan<ComponentId> components)
        {
            Entity entity;
            if (_entities.TryGetRetainedEntity(parameters.SyncId, out entity) && _world.IsAlive(entity))
            {
                _world.Remove<PendingDestroy>(entity);
                _world.Add(entity, parameters.SyncId, parameters.SyncIdReserve, parameters.RandomSeed, new Alive());
                _world.Add(entity, components);
            }
            else
            {
                Span<ComponentId> initial = stackalloc ComponentId[_components.Length + components.Length];
                _components.AsSpan().CopyTo(initial);
                components.CopyTo(initial.Slice(_components.Length));
                entity = _world.Create(initial);
                _world.Set(entity, parameters.SyncId, parameters.SyncIdReserve, parameters.RandomSeed, new Alive());
            }

            _entityRegistryService.Register(entity, parameters.SyncId);
            return entity;
        }
    }
}
