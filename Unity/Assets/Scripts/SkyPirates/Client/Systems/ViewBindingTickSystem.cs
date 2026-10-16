using Delta.ECS;
using Delta.ECS.Unity;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Services.Netcode;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using ConfigViewToken = DVG.SkyPirates.Shared.Components.Config.ViewToken;
using BindingViewToken = Delta.ECS.Unity.ViewToken;

namespace DVG.SkyPirates.Client.Systems
{
    public sealed class ViewBindingTickSystem : ITickableExecutor
    {
        private readonly World _world;
        private readonly SceneWorld _scene;
        private readonly ViewBindingSystem _binding;
        private readonly Dictionary<Entity, GameObject> _views = new();
        private readonly List<Entity> _tokens = new();
        private readonly List<Entity> _unbind = new();
        private readonly List<Entity> _remove = new();
        private readonly IOperation _collectTokens;
        private readonly IOperation _collectBound;
        private bool _rebind;
        private Entity[] _batch = Array.Empty<Entity>();

        public ViewBindingTickSystem(World world, SceneWorld scene, ViewBindingSystem binding, SkyPiratesSessionProvider session)
        {
            _world = world;
            _scene = scene;
            _binding = binding;
            Query configTokens = world.WhereAll<ConfigViewToken>().WhereNone<PendingDestroy>();
            Query bound = world.WhereAll<ViewBound>();
            session.StateRestored += () => _rebind = true;
            _collectTokens = _world.ForEachEntity(in configTokens, (EntityRef entity) =>
            {
                if (!entity.Has<BindingViewToken>()) _tokens.Add(entity);
            });
            _collectBound = _world.ForEachEntity(in bound, (EntityRef entity) =>
            {
                if (_scene.TryGetView(entity, out GameObject view)) _views[entity] = view;
            });
        }

        public void Tick(int tick)
        {
            _tokens.Clear();
            _unbind.Clear();
            _remove.Clear();
            foreach (var pair in _views)
            {
                Entity entity = pair.Key;
                GameObject view = pair.Value;
                if (!_world.IsAlive(entity))
                {
                    if (view != null) { view.SetActive(false); Object.Destroy(view); }
                    _scene.UnbindView(entity);
                    _remove.Add(entity);
                    continue;
                }
                bool active = !_world.Has<PendingDestroy>(entity) && _world.Has<ConfigViewToken>(entity);
                if (view != null && view.activeSelf != active) view.SetActive(active);
                if (!active)
                {
                    _tokens.Add(entity);
                    _unbind.Add(entity);
                }
                else if (_rebind || !_world.Has<ViewBound>(entity)) _unbind.Add(entity);
            }
            foreach (Entity entity in _remove) _views.Remove(entity);
            // The library binder handles normal OnBind, including reuse of a retained GameObject.
            _world.Remove<BindingViewToken>(Batch(_tokens));
            _world.Remove<ViewBound>(Batch(_unbind));
            _tokens.Clear();
            _collectTokens.Invoke();
            foreach (Entity entity in _tokens)
                _world.Add(entity, new BindingViewToken(_world.Get<ConfigViewToken>(entity).Value));
            _binding.BindPendingViews();
            _collectBound.Invoke();
            _rebind = false;
        }
        private ReadOnlySpan<Entity> Batch(List<Entity> entities)
        {
            if (_batch.Length < entities.Count) Array.Resize(ref _batch, Math.Max(entities.Count, _batch.Length * 2));
            entities.CopyTo(_batch);
            return _batch.AsSpan(0, entities.Count);
        }
    }
}
