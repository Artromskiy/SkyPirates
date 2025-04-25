namespace DVG.Core
{
    public interface IFactory { }
    public interface IFactory<T> : IFactory
    {
        public T Create();
    }
    public interface IFactory<T, P> : IFactory
    {
        public T Create(P parameters);
    }

}
