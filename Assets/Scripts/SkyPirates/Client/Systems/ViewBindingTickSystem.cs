using Delta.ECS;
using Delta.ECS.Unity;
using ConfigViewToken = DVG.SkyPirates.Shared.Components.Config.ViewToken;
using BindingViewToken = Delta.ECS.Unity.ViewToken;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.Views.Components;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;
using UnityEngine;

namespace DVG.SkyPirates.Client.Systems
{
    public sealed class ViewBindingTickSystem : ITickableExecutor
    {
        private const string HealthbarPrefabPath = "Prefabs/UI/HealthbarView";

        private readonly World _world;
        private readonly ViewBindingSystem _viewBinding;
        private readonly IHealthbarCanvasVM _healthbarCanvas;
        private readonly HashSet<Entity> _healthbars = new();
        private readonly List<Entity> _pendingTokens = new();
        private readonly List<Entity> _pendingHealthbars = new();
        private readonly GameObject _healthbarPrefab;
        private readonly Query _configViewTokenEntities;
        private readonly Query _healthbarEntities;

        public ViewBindingTickSystem(
            World world,
            ViewBindingSystem viewBinding,
            IHealthbarCanvasVM healthbarCanvas)
        {
            _world = world;
            _viewBinding = viewBinding;
            _healthbarCanvas = healthbarCanvas;
            _healthbarPrefab = Resources.Load<GameObject>(HealthbarPrefabPath);
            _configViewTokenEntities = _world.WhereAll(stackalloc ComponentId[1]
            {
                _world.Layouts.GetPrimary<ConfigViewToken>()
            }).WhereNone(stackalloc ComponentId[1]
            {
                _world.Layouts.GetPrimary<BindingViewToken>()
            });
            _healthbarEntities = _world.WhereAll(stackalloc ComponentId[3]
            {
                _world.Layouts.GetPrimary<Health>(),
                _world.Layouts.GetPrimary<MaxHealth>(),
                _world.Layouts.GetPrimary<Position>()
            });
        }

        public void Tick(int tick)
        {
            _pendingTokens.Clear();
            _pendingHealthbars.Clear();
            _world.ForEachEntity(in _configViewTokenEntities, CollectTokens).Invoke();
            _world.ForEachEntity(in _healthbarEntities, CollectHealthbars).Invoke();

            for (int i = 0; i < _pendingTokens.Count; i++)
            {
                Entity entity = _pendingTokens[i];
                _world.Add(entity, new BindingViewToken(_world.Get<ConfigViewToken>(entity).Value));
            }

            for (int i = 0; i < _pendingHealthbars.Count; i++)
            {
                Entity entity = _pendingHealthbars[i];
                CreateHealthbar(entity);
            }

            _viewBinding.BindPendingViews();
            _pendingTokens.Clear();
            _pendingHealthbars.Clear();
            _healthbars.RemoveWhere(entity => !_world.IsAlive(entity));
        }

        private void CollectTokens(EntityRef entityRef)
        {
            Entity entity = entityRef;
            _pendingTokens.Add(entity);
        }

        private void CollectHealthbars(EntityRef entityRef)
        {
            Entity entity = entityRef;
            if (_healthbarPrefab != null && _healthbars.Add(entity))
                _pendingHealthbars.Add(entity);
        }

        private void CreateHealthbar(Entity entity)
        {
            if (_healthbarPrefab == null)
                return;

            GameObject instance = Object.Instantiate(_healthbarPrefab);
            ComponentView[] views = instance.GetComponents<ComponentView>();
            for (int i = 0; i < views.Length; i++)
            {
                views[i].Bind(_world, entity);
            }

            _healthbarCanvas.RegisterView(instance.transform);
        }
    }
}
