using DVG.Commands;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.IServices;

namespace DVG.SkyPirates.Local.Services
{
    public class LocalCommandSendService : ICommandSender
    {
        private readonly ICommandReciever _comandReciever;

        public LocalCommandSendService(ICommandReciever comandReciever)
        {
            _comandReciever = comandReciever;
        }

        public void SendCommand<T>(Command<T> cmd)
        {
            if (CommandsRegistry.IsPredicted<T>())
                return;

            _comandReciever.InvokeCommand(cmd);
        }
    }
}
