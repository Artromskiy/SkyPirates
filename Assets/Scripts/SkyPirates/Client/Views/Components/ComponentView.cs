using Delta.ECS;
using Delta.ECS.Unity;
using DVG.Components;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Runtime;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public abstract class ComponentView : MonoBehaviour, IDisposable
    {
        private World _world;
        private Entity _entity;
        private bool _initialized;
        private bool _disabled;
        private bool _disposed;

        protected World World => _world;
        protected Entity Entity => _entity;
        protected bool IsAlive => _world != null && _world.IsAlive(_entity);

        public virtual void OnBound() { }
        public virtual void Tick() { }
        public virtual void Dispose() { }

        protected bool Has<T>() where T : struct => IsAlive && World.Has<T>(Entity);
        protected T Get<T>() where T : struct => World.Get<T>(Entity);
        protected ref T Set<T>() where T : struct => ref World.GetRef<T>(Entity);

        public void Bind(EntityContext context)
        {
            if (context.IsAlive)
                Bind(context.World, context.Entity);
        }

        public void Bind(World world, Entity entity)
        {
            if (_initialized || world == null || !world.IsAlive(entity))
                return;

            _world = world;
            _entity = entity;
            _initialized = true;
            OnBound();
        }

        private void Start()
        {
            if (this.TryGetEntity(out EntityContext context))
                Bind(context);
        }

        private void Update()
        {
            if (!_initialized)
                return;

            if (!IsAlive)
            {
                DisposeOnce();
                Destroy(gameObject);
                return;
            }

            bool disabled = Has<Disabled>();
            if (_disabled == disabled && disabled)
                return;

            _disabled = disabled;
            Tick();
        }

        protected virtual void OnDestroy() => DisposeOnce();

        private void DisposeOnce()
        {
            if (_disposed)
                return;

            _disposed = true;
            Dispose();
        }

        protected event Action OnEditorChanged
        {
            add => EditorChanges.SubscribeOnChanged(this, value);
            remove => EditorChanges.UnsubscribeOnChanged(this, value);
        }
    }
}
