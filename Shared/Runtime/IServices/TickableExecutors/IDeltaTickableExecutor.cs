using Delta;
namespace DVG.SkyPirates.Shared.IServices.TickableExecutors
{
    public interface IDeltaTickable
    {
        void Tick(int tick, fix deltaTime);
    }

    public interface IDeltaTickableExecutor : IDeltaTickable { }

    public interface ITransientDeltaTickableExecutor : IDeltaTickableExecutor { }
}
