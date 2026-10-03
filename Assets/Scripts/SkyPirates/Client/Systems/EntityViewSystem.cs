using Delta.ECS;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DVG.SkyPirates.Client.Systems
{
    public class EntityViewSystem : ITickableExecutor
    {
        private readonly HashSet<Entity> _created = new();
        private readonly World _world;
        private readonly IEntityVMFactory _vmFactory;
        private readonly IEntityViewProvider[] _viewProviders;
        private Query? _allEntitiesCache;
        private Query _allEntities => _allEntitiesCache ??= _world.WhereAll(ReadOnlySpan<ComponentId>.Empty);

        public EntityViewSystem(World world, IEntityVMFactory vmFactory, IEnumerable<IEntityViewProvider> viewProviders)
        {
            _world = world;
            _vmFactory = vmFactory;
            _viewProviders = viewProviders.ToArray();
        }

        public void Tick(int tick)
        {
            var query = new CreateVMQuery(_world, _created, _vmFactory, _viewProviders);
            var allEntities = _allEntities;
            _world.ForEachEntity(in allEntities, query.Invoke);
        }

        private sealed class CreateVMQuery
        {
            private readonly World _world;
            private readonly HashSet<Entity> _created;
            private readonly IEntityVMFactory _vmFactory;
            private readonly IEntityViewProvider[] _viewProviders;

            public CreateVMQuery(World world, HashSet<Entity> created, IEntityVMFactory vmFactory, IEntityViewProvider[] viewProviders)
            {
                _world = world;
                _created = created;
                _vmFactory = vmFactory;
                _viewProviders = viewProviders;
            }

            public void Invoke(Entity entity)
            {
                if (!_created.Add(entity))
                    return;
                var vm = _vmFactory.Create((_world, entity));
                foreach (var viewProvider in _viewProviders)
                {
                    if (!viewProvider.TryCreateView(vm, out var view))
                        continue;
                    view.ViewModel = vm;
                }
            }
        }
    }
}
