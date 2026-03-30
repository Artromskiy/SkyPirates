#nullable enable
using DVG.Collections;
using DVG.Commands;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.Services;
using Riptide;
using System;

namespace DVG.SkyPirates.Client.Services
{
    internal class CommandReciever : ICommandReciever
    {
        private readonly Riptide.Client? _client;
        private readonly MessageIO _messageIO;
        private readonly GenericCollection _listeners = new();

        public CommandReciever(Riptide.Client client)
        {
            _messageIO = new MessageIO();
            _client = client;

            _client.MessageReceived += OnMessageRecieved;
        }

        private void OnMessageRecieved(object? _, MessageReceivedEventArgs e)
        {
            var caller = new Caller(e.FromConnection.Id, e.Message, _messageIO, this);
            CommandsRegistry.Call(e.MessageId, ref caller);
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

        public void InvokeCommand<T>(Command<T> command)
        {
            if (_listeners.TryGet<Action<Command<T>>>(out var callback))
                callback.Invoke(command);
        }


        private readonly struct Caller : IGenericAction
        {
            private readonly int _clientId;
            private readonly Message _message;
            private readonly MessageIO _messageIO;
            private readonly CommandReciever _recieveService;

            public Caller(int clientId, Message message, MessageIO messageIO, CommandReciever recieveService)
            {
                _clientId = clientId;
                _message = message;
                _messageIO = messageIO;
                _recieveService = recieveService;
            }

            public void Invoke<T>()
            {
                if (!_messageIO.RecieveMessage<T>(_message, _clientId, out var command))
                    return;
                _recieveService.InvokeCommand(command);
            }
        }
    }
}
