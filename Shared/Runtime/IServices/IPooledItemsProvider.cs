namespace DVG.SkyPirates.Shared.IServices
{
    public interface IPooledItemsProvider
    {
        T Get<T>() where T : new();
        void Return<T>(T value) where T : new();
    }
}
