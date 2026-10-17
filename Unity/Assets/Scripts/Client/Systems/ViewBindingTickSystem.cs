using Delta.ECS;
using Delta.ECS.Unity;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Services.Netcode;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using ConfigViewToken = SlyPirates.Shared.Components.Config.ViewToken;
using BindingViewToken = Delta.ECS.Unity.ViewToken;

namespace SlyPirates.Client.Systems
{
    public sealed class ViewBindingTickSystem : ITickableExecutor
    {
        private readonly World _world;
        private readonly SceneWorld _scene;
        private readonly AddressableViewBindingSystem _binding;
        private readonly HashSet<Entity> _changed = new();
        private readonly HashSet<Entity> _bound = new();
        private readonly List<Entity> _tokens = new();
        private readonly List<Entity> _unbind = new();
        private readonly IOperation _tokenChanges;
        private readonly IOperation _lifetimeChanges;
        private readonly IOperation _bindingChanges;
        private readonly IOperation _collectRebind;
        private bool _rebind;
        private Entity[] _batch = Array.Empty<Entity>();

        public ViewBindingTickSystem(World world, SceneWorld scene, AddressableViewBindingSystem binding, SkyPiratesSessionProvider session)
        {
            _world = world;
            _scene = scene;
            _binding = binding;
            _tokenChanges = world.ForEachChange<ConfigViewToken>(world.Components.GetPrimary<ConfigViewToken>(), OnChanged);
            _lifetimeChanges = world.ForEachChange<PendingDestroy>(world.Components.GetPrimary<PendingDestroy>(), OnChanged);
            _bindingChanges = world.ForEachChange<ViewBound>(world.Components.GetPrimary<ViewBound>(), OnBindingChanged);
            Query bound = world.WhereAll<ViewBound, ConfigViewToken>().WhereNone<PendingDestroy>();
            _collectRebind = world.ForEachEntity(in bound, (EntityRef entity) => _changed.Add(entity));
            session.StateRestored += () => _rebind = true;
        }

        public void Tick(int tick)
        {
            _tokens.Clear();
            _unbind.Clear();
            _tokenChanges.Invoke();
            _lifetimeChanges.Invoke();
            _bindingChanges.Invoke();
            if (_rebind)
            {
                _collectRebind.Invoke();
                _rebind = false;
            }

            foreach (Entity entity in _changed)
            {
                if (!_world.IsAlive(entity))
                {
                    if (_scene.TryGetView(entity, out GameObject view))
                    {
                        view.SetActive(false);
                        if (!_binding.ReleaseView(entity, view))
                        {
                            Object.Destroy(view);
                        }
                    }
                    _scene.UnbindView(entity);
                    continue;
                }

                bool hasViewToken = _world.Has<ConfigViewToken>(entity);
                if (_world.Has<PendingDestroy>(entity) || !hasViewToken)
                {
                    if (_world.Has<BindingViewToken>(entity))
                    {
                        _tokens.Add(entity);
                    }

                    if (hasViewToken)
                    {
                        // A gameplay destroy keeps its bound view alive for its death animation.
                        continue;
                    }

                    // A history restore can remove the view's source components. Suspend the
                    // existing view until those components return, then bind it again in place.
                    if (_world.Has<ViewBound>(entity))
                    {
                        _unbind.Add(entity);
                    }

                    if (_scene.TryGetView(entity, out GameObject staleView))
                    {
                        staleView.SetActive(false);
                    }

                    continue;
                }

                var token = new BindingViewToken(_world.Get<ConfigViewToken>(entity).Value);
                if (_world.Has<BindingViewToken>(entity))
                {
                    _world.GetRef<BindingViewToken>(entity) = token;
                }
                else
                {
                    _world.Add(entity, token);
                }

                if (_world.Has<ViewBound>(entity))
                {
                    _unbind.Add(entity);
                }
            }
            _changed.Clear();

            // The library binder handles normal OnBind, including reuse of a retained GameObject.
            if (_tokens.Count != 0)
            {
                _world.Remove<BindingViewToken>(Batch(_tokens));
            }

            if (_unbind.Count != 0)
            {
                _world.Remove<ViewBound>(Batch(_unbind));
            }

            _binding.BindPendingViews();
            _bindingChanges.Invoke();
            foreach (Entity entity in _bound)
            {
                if (_world.IsAlive(entity) && !_world.Has<PendingDestroy>(entity) && _world.Has<ConfigViewToken>(entity)
                    && _scene.TryGetView(entity, out GameObject view) && !view.activeSelf)
                {
                    view.SetActive(true);
                }
            }
            _bound.Clear();
        }

        private void OnChanged<T>(Entity entity, ChangeKind change, ref T component) => _changed.Add(entity);

        private void OnBindingChanged(Entity entity, ChangeKind change, ref ViewBound bound)
        {
            if (change == ChangeKind.Removed)
            {
                _changed.Add(entity);
            }
            else
            {
                _bound.Add(entity);
            }
        }

        private ReadOnlySpan<Entity> Batch(List<Entity> entities)
        {
            if (_batch.Length < entities.Count)
            {
                Array.Resize(ref _batch, Math.Max(entities.Count, _batch.Length * 2));
            }

            entities.CopyTo(_batch);
            return _batch.AsSpan(0, entities.Count);
        }
    }
}
