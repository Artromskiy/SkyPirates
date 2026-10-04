using DVG.Collections;
using Delta.Netcode;
using DVG.SkyPirates.Shared.IServices;
using System;

namespace DVG.SkyPirates.Local.Services
{
    public class LocalCommandReciever : ICommandReciever
    {
        private readonly ICommandAcceptanceService _commandAcceptance;
        private readonly GenericCollection _listeners = new();

        public LocalCommandReciever(ICommandAcceptanceService commandAcceptance)
        {
            _commandAcceptance = commandAcceptance;
        }

        public void InvokeCommand<T>(Command<T> command)
        {
            _commandAcceptance.PrepareLocal(in command, out var prepared);

            if (_listeners.TryGet<Action<Command<T>>>(out var callback))
                callback.Invoke(prepared);
        }

        public void RegisterReciever<T>(Action<Command<T>> reciever)
        {
            if (!_listeners.TryGet<Action<Command<T>>>(out var callback))
                _listeners.Add(reciever);
            else
                _listeners.Add(callback + reciever);
        }

        public void UnregisterReciever<T>(Action<Command<T>> reciever)
        {
            if (!_listeners.TryGet<Action<Command<T>>>(out var recievers))
                return;
            recievers -= reciever;
            if (recievers == null)
                _listeners.Remove<Action<Command<T>>>();
            else
                _listeners.Add(reciever);
        }



        private class ActionContainer<T> : IActionContainer
        {
            public event Action<Command<T>>? Recievers;
            public bool HasTargets => Recievers?.GetInvocationList().Length > 0;

            public void Invoke(Command<T> cmd)
            {
                Recievers?.Invoke(cmd);
            }
        }

        private interface IActionContainer { }
    }
}
