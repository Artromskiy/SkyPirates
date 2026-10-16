using Delta;
namespace SlyPirates.Shared.IServices.TickableExecutors
{
    public interface IDeltaTickable
    {
        void Tick(int tick, fix deltaTime);
    }

    public interface IDeltaTickableExecutor : IDeltaTickable { }
}
