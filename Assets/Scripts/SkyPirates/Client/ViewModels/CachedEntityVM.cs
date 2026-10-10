using Delta.ECS;
using DVG.Collections;
using DVG.Components;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Components.Framed;
using UnityEngine;

namespace DVG.SkyPirates.Client.ViewModels
{
    public class CachedEntityVM : IEntityVM
    {
        private readonly World _world;
        private readonly Entity _entity;
        private int _cachedFrame = -1;

        private readonly GenericCollection _cache = new();

        public bool Disposed => !_world.IsAlive(_entity);
        public bool Disabled => Has<Disabled>();
        public bool Alive => Has<Alive>();

        public CachedEntityVM(
            World world,
            Entity entity)
        {
            _world = world;
            _entity = entity;
        }

        private void EnsureFresh()
        {
            int currentFrame = Time.frameCount;
            if (_cachedFrame == currentFrame)
                return;

            _cache.Clear();
            _cachedFrame = currentFrame;
        }

        public bool Has<T>()
        {
            EnsureFresh();

            if (_cache.TryGet<T>(out _))
            {
                return true;
            }
            if (_world.TryGet<T>(_entity, out var value))
            {
                _cache.Add(value);
                return true;
            }
            return false;
        }

        public T Get<T>()
        {
            EnsureFresh();

            if (!_cache.TryGet<T>(out var value))
                _cache.Add(value = _world.Get<T>(_entity));

            return value;
        }

        public ref T Set<T>() =>
#if UNITY_EDITOR
            ref _world.GetRef<T>(_entity);
#else
            throw new System.InvalidOperationException(
                $"Attempt to write {typeof(T).Name} from ViewModel in runtime mode");
#endif

        public void Dispose()
        {
#if UNITY_EDITOR
            _world.Destroy(_entity);
#else
            throw new System.InvalidOperationException(
                $"Attempt to Destroy {_entity} from ViewModel in runtime mode");
#endif
        }
    }
}
