using DVG.Core;

namespace DVG.SkyPirates.Client.IFactories
{
    public interface IPathViewFactory<T> : IFactory<T, string> where T : IView { }
}
