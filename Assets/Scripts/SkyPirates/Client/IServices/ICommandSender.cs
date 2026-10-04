#nullable enable
using Delta.Netcode;

namespace DVG.SkyPirates.Client.IServices
{
    public interface ICommandSender
    {
        void SendCommand<T>(Command<T> cmd);
    }
}
