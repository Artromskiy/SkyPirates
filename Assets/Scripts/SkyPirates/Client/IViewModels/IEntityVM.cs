using DVG.Core;

namespace DVG.SkyPirates.Client.IViewModels
{
    public interface IEntityVM : IViewModel
    {
        bool Has<T>();
        T Get<T>();
        ref T Set<T>();
        void Dispose();
        bool Disabled { get; }
        bool Disposed { get; }
        bool Alive { get; }
    }
}
