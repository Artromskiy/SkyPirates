using Delta.ECS;
using Delta.ECS.Unity;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DVG.SkyPirates.Client.Systems
{
    public sealed class EntityViewSystem : ITickableExecutor
    {
        private readonly HashSet<Entity> _created = new();
        private readonly List<PendingEntityView> _pending = new();
        private readonly World _world;
        private readonly IEntityVMFactory _vmFactory;
        private readonly IEntityViewProvider[] _viewProviders;
        private readonly ViewBindingSystem _viewBinding;
        private Query? _allEntitiesCache;
        private Query _allEntities => _allEntitiesCache ??= _world.WhereAll(ReadOnlySpan<ComponentId>.Empty);

        public EntityViewSystem(
            World world,
            IEntityVMFactory vmFactory,
            IEnumerable<IEntityViewProvider> viewProviders,
            ViewBindingSystem viewBinding)
        {
            _world = world;
            _vmFactory = vmFactory;
            _viewProviders = viewProviders.ToArray();
            _viewBinding = viewBinding;
        }

        public void Tick(int tick)
        {
            _pending.Clear();
            var collect = new CollectEntityViews(_world, _created, _vmFactory, _pending);
            var allEntities = _allEntities;
            _world.ForEachEntity(in allEntities, collect.Invoke).Invoke();

            for (int i = 0; i < _pending.Count; i++)
            {
                PendingEntityView pending = _pending[i];
                if (pending.ViewId != null && !_world.Has<ViewToken>(pending.Entity))
                {
                    _world.Add(pending.Entity, new ViewToken(pending.ViewId));
                }

                if (pending.ViewId != null || _world.Has<ViewToken>(pending.Entity))
                {
                    EntityViewModelRegistry.Register(_world, pending.Entity, pending.ViewModel);
                }

                for (int providerIndex = 0; providerIndex < _viewProviders.Length; providerIndex++)
                {
                    if (_viewProviders[providerIndex].TryCreateView(pending.ViewModel, out var view))
                    {
                        view.ViewModel = pending.ViewModel;
                    }
                }
            }

            _viewBinding.BindPendingViews();
            _pending.Clear();
        }

        private readonly struct PendingEntityView
        {
            public readonly Entity Entity;
            public readonly IEntityVM ViewModel;
            public readonly string ViewId;

            public PendingEntityView(Entity entity, IEntityVM viewModel, string viewId)
            {
                Entity = entity;
                ViewModel = viewModel;
                ViewId = viewId;
            }
        }

        private sealed class CollectEntityViews
        {
            private readonly World _world;
            private readonly HashSet<Entity> _created;
            private readonly IEntityVMFactory _vmFactory;
            private readonly List<PendingEntityView> _pending;

            public CollectEntityViews(World world, HashSet<Entity> created, IEntityVMFactory vmFactory, List<PendingEntityView> pending)
            {
                _world = world;
                _created = created;
                _vmFactory = vmFactory;
                _pending = pending;
            }

            public void Invoke(EntityRef entityRef)
            {
                Entity entity = entityRef;
                if (!_created.Add(entity))
                {
                    return;
                }

                IEntityVM viewModel = _vmFactory.Create((_world, entity));
                string viewId = _world.Has<ViewId>(entity) ? _world.Get<ViewId>(entity).Value : null;
                _pending.Add(new PendingEntityView(entity, viewModel, viewId));
            }
        }
    }

    internal static class EntityViewModelRegistry
    {
        private static readonly Dictionary<(World World, Entity Entity), IEntityVM> ViewModels = new();

        public static void Register(World world, Entity entity, IEntityVM viewModel)
            => ViewModels[(world, entity)] = viewModel;

        public static bool TryGet(World world, Entity entity, out IEntityVM viewModel)
            => ViewModels.TryGetValue((world, entity), out viewModel);

        public static void Remove(World world, Entity entity)
            => ViewModels.Remove((world, entity));
    }
}
