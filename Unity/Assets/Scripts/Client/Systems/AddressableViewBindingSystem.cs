using Delta.ECS;
using Delta.ECS.Unity;
using System;
using System.Collections.Generic;
using SlyPirates.Client;
using UnityEngine;

namespace SlyPirates.Client.Systems
{
    public sealed class AddressableViewBindingSystem
    {
        private readonly SceneWorld _scene;
        private readonly World _world;
        private readonly UnityEngine.Object _owner;
        private readonly ComponentId _viewTokenId;
        private readonly ComponentId _viewBoundId;
        private readonly EcsOperation<ForEachEntityOperationInvoker> _collectPending;
        private readonly List<Entity> _pending = new();
        private readonly List<MonoBehaviour> _viewBehaviours = new();
        private readonly HashSet<Entity> _loading = new();
        private readonly Dictionary<Entity, GameObject> _instances = new();

        public AddressableViewBindingSystem(SceneWorld scene, UnityEngine.Object owner)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _world = scene.World;
            _owner = owner;
            _viewTokenId = _world.Components.GetPrimary<ViewToken>();
            _viewBoundId = _world.Components.GetPrimary<ViewBound>();
            Query query = _world.WhereAll(stackalloc ComponentId[1] { _viewTokenId })
                .WhereNone(stackalloc ComponentId[1] { _viewBoundId });
            _collectPending = _world.ForEachEntity(in query, QueuePending);
        }

        public void BindPendingViews()
        {
            _pending.Clear();
            _collectPending.Invoke();
            for (int i = 0; i < _pending.Count; i++)
            {
                BeginBinding(_pending[i]);
            }
        }

        public bool ReleaseView(Entity entity)
        {
            if (!_instances.Remove(entity, out GameObject view))
            {
                return false;
            }

            AddressableLoader.ReleaseInstance(view);

            return true;
        }

        public bool ReleaseView(Entity entity, GameObject view)
        {
            _scene.UnbindView(entity);
            return ReleaseView(entity);
        }

        private void QueuePending(EntityRef entity) => _pending.Add(entity);

        private async void BeginBinding(Entity entity)
        {
            if (!_world.IsAlive(entity) || _world.Has(entity, _viewBoundId) || _loading.Contains(entity))
            {
                return;
            }

            if (_scene.TryGetView(entity, out GameObject existingView))
            {
                BindExistingView(existingView, entity);
                return;
            }

            _loading.Add(entity);
            string key = _world.Get<ViewToken>(entity).Id;
            GameObject view = await AddressableLoader.Instantiate(key, null, _owner);
            _loading.Remove(entity);
            if (view == null || !_world.IsAlive(entity) || _world.Has(entity, _viewBoundId)
                || !_world.Has(entity, _viewTokenId) || _world.Get<ViewToken>(entity).Id != key)
            {
                AddressableLoader.ReleaseInstance(view);
                return;
            }

            if (!_scene.BindView(entity, view))
            {
                AddressableLoader.ReleaseInstance(view);
                return;
            }

            _instances[entity] = view;
            _world.Add(entity, _viewBoundId);
            NotifyViewBound(view, entity);
        }

        private void BindExistingView(GameObject view, Entity entity)
        {
            _world.Add(entity, _viewBoundId);
            NotifyViewBound(view, entity);
        }

        private void NotifyViewBound(GameObject view, Entity entity)
        {
            _viewBehaviours.Clear();
            view.GetComponentsInChildren(true, _viewBehaviours);
            EntityContext context = view.GetEntity();
            for (int i = 0; i < _viewBehaviours.Count; i++)
            {
                if (_viewBehaviours[i] is IEntityView entityView)
                {
                    entityView.OnBind(context);
                }
            }

            _viewBehaviours.Clear();
        }
    }
}
