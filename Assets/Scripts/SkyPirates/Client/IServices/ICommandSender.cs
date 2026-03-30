#nullable enable
using DVG.Commands;

namespace DVG.SkyPirates.Client.IServices
{
    public interface ICommandSender
    {
        void SendCommand<T>(Command<T> cmd);
    }
}
