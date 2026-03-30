using Arch.Core;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;
using System.Linq;

namespace DVG.SkyPirates.Client.Systems
{
    public class EntityViewSystem : ITickableExecutor
    {
        private readonly HashSet<Entity> _created = new();
        private readonly World _world;
        private readonly IEntityVMFactory _vmFactory;
        private readonly IEntityViewProvider[] _viewProviders;

        public EntityViewSystem(World world, IEntityVMFactory vmFactory, IEnumerable<IEntityViewProvider> viewProviders)
        {
            _world = world;
            _vmFactory = vmFactory;
            _viewProviders = viewProviders.ToArray();
        }

        public void Tick(int tick)
        {
            var query = new CreateVMQuery(_world, _created, _vmFactory, _viewProviders);
            _world.InlineQuery(QueryDescription.Null, ref query);
        }

        private readonly struct CreateVMQuery : IForEach
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

            public void Update(Entity entity)
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
