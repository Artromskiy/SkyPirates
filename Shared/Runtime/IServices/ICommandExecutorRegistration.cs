using Delta.Netcode;

namespace DVG.SkyPirates.Shared.IServices
{
    public interface ICommandExecutorRegistration
    {
    }

    public interface ITransientCommandInput<T>
    {
        void ApplyTransient(in T input);
    }
}
