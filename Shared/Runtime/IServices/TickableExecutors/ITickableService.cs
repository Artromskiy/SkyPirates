namespace DVG.SkyPirates.Shared.IServices.TickableExecutors
{
    public interface ITickableService<T> where T : ITickable
    {
        void Tick(int tick);
    }
}
