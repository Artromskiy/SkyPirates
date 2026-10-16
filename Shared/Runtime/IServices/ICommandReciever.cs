using Delta.Netcode;
using System;

namespace SlyPirates.Shared.IServices
{
    public interface ICommandReciever
    {
        void RegisterReciever<T>(Action<Command<T>> reciever);
        void UnregisterReciever<T>(Action<Command<T>> reciever);
        void InvokeCommand<T>(Command<T> command);
    }
}
