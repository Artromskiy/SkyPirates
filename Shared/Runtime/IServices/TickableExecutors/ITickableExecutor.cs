namespace DVG.SkyPirates.Shared.IServices.TickableExecutors
{
    public interface ITickableExecutor :
        ITickable,
        IPreTickable,
        IPostTickable,
        IInTickable
    { }

    public interface ITickable
    {
        void Tick(int tick);
    }

    public interface IPreTickable : ITickable { }
    public interface IPostTickable : ITickable { }
    public interface IInTickable : ITickable { }
}
