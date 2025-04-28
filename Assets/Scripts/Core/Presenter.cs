namespace DVG.Core
{
    public class Presenter : IPresenter { }
    public class Presenter<V, M> : Presenter, IPresenter<V, M>
        where V : IView
    {
        public V View { get; set; }
        public M Model { get; set; }

        public Presenter(V view, M model)
        {
            View = view;
            Model = model;
        }
    }
}