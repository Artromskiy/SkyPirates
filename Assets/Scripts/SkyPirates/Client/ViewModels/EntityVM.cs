using Arch.Core;
using DVG.Components;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Components.Framed;

namespace DVG.SkyPirates.Client.ViewModels
{
    public class EntityVM : IEntityVM
    {
        private readonly World _world;
        private readonly Entity _entity;

        public bool Disposed => !_world.IsAlive(_entity);
        public bool Alive => Has<Alive>();
        public bool Disabled => Has<Disabled>();

        public EntityVM(World world, Entity entity)
        {
            _world = world;
            _entity = entity;
        }

        public bool Has<T>() => _world.Has<T>(_entity);
        public T Get<T>() => _world.Get<T>(_entity);

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