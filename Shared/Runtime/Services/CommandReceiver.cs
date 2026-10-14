using DVG.Collections;
using Delta.Netcode;
using DVG.SkyPirates.Shared.IServices;
using System;

namespace DVG.SkyPirates.Shared.Services
{
    internal sealed class CommandReceiver : ICommandReciever
    {
        private readonly GenericCollection _listeners = new();

        public CommandReceiver() { }

        public void InvokeCommand<T>(Command<T> command)
        {
            if (_listeners.TryGet<Action<Command<T>>>(out var callback))
            {
                callback.Invoke(command);
            }
        }

        public void RegisterReciever<T>(Action<Command<T>> receiver)
        {
            if (!_listeners.TryGet<Action<Command<T>>>(out var callback))
            {
                _listeners.Add(receiver);
            }
            else
            {
                _listeners.Add(callback + receiver);
            }
        }

        public void UnregisterReciever<T>(Action<Command<T>> receiver)
        {
            if (!_listeners.TryGet<Action<Command<T>>>(out var callbacks))
            {
                return;
            }

            callbacks -= receiver;
            if (callbacks == null)
            {
                _listeners.Remove<Action<Command<T>>>();
            }
            else
            {
                _listeners.Add(callbacks);
            }
        }
    }
}
