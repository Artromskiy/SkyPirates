using Delta;
namespace SlyPirates.Shared.IServices.TickableExecutors
{
    public interface IDeltaTickableService<T> where T : IDeltaTickable
    {
        void Tick(int tick, fix deltaTime);
    }
}
