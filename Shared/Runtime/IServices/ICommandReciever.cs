using Delta.Netcode;
using System;

namespace DVG.SkyPirates.Shared.IServices
{
    public interface ICommandReciever
    {
        void RegisterReciever<T>(Action<Command<T>> reciever);
        void UnregisterReciever<T>(Action<Command<T>> reciever);
        void InvokeCommand<T>(Command<T> command);
    }
}
