namespace DVG.Core
{
    public interface IPresenter { }
    public interface IPresenter<V, M> : IPresenter
        where V : IView
    {
        public V View { get; set; }
        public M Model { get; set; }
    }
}