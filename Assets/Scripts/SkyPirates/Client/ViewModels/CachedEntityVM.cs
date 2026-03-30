using Arch.Core;
using DVG.Collections;
using DVG.Components;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Client.ViewModels
{
    public class CachedEntityVM : IEntityVM
    {
        private readonly World _world;
        private readonly Entity _entity;
        private readonly ITickCounterService _tickCounter;

        private int _cachedTick = -1;

        private readonly GenericCollection _cache = new();

        public bool Disposed => !_world.IsAlive(_entity);
        public bool Disabled => Has<Disabled>();
        public bool Alive => Has<Alive>();

        public CachedEntityVM(
            ITickCounterService tickCounter,
            World world,
            Entity entity)
        {
            _tickCounter = tickCounter;
            _world = world;
            _entity = entity;
        }

        private void EnsureFresh()
        {
            var currentTick = _tickCounter.TickCounter;

            if (_cachedTick == currentTick)
                return;

            _cache.Clear();
            _cachedTick = currentTick;
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
            ref _world.Get<T>(_entity);
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