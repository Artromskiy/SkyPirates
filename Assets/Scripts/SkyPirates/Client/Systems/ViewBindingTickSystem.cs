using Delta.ECS;
using Delta.ECS.Unity;
using ConfigViewToken = DVG.SkyPirates.Shared.Components.Config.ViewToken;
using BindingViewToken = Delta.ECS.Unity.ViewToken;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace DVG.SkyPirates.Client.Systems
{
    public sealed class ViewBindingTickSystem : ITickableExecutor
    {
        private readonly World _world;
        private readonly ViewBindingSystem _viewBinding;
        private readonly List<Entity> _pendingTokens = new();
        private readonly Query _configViewTokenEntities;

        public ViewBindingTickSystem(World world, ViewBindingSystem viewBinding)
        {
            _world = world;
            _viewBinding = viewBinding;
            _configViewTokenEntities = _world.WhereAll(stackalloc ComponentId[1]
            {
                _world.Layouts.GetPrimary<ConfigViewToken>()
            }).WhereNone(stackalloc ComponentId[1]
            {
                _world.Layouts.GetPrimary<BindingViewToken>()
            });
        }

        public void Tick(int tick)
        {
            _pendingTokens.Clear();
            _world.ForEachEntity(in _configViewTokenEntities, CollectToken).Invoke();
            for (int i = 0; i < _pendingTokens.Count; i++)
                AddBindingToken(_pendingTokens[i]);
            _viewBinding.BindPendingViews();
            _pendingTokens.Clear();
        }

        private void CollectToken(EntityRef entityRef)
        {
            _pendingTokens.Add(entityRef);
        }

        private void AddBindingToken(Entity entity)
        {
            _world.Add(entity, new BindingViewToken(_world.Get<ConfigViewToken>(entity).Value));
        }
    }
}
