using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.IServices;
using Riptide;
using System;
using System.Collections.Generic;

namespace DVG.SkyPirates.Client.Services
{
    internal class CommandRecieveService : ICommandRecieveService
    {
        private readonly Riptide.Client _client;
        private readonly ICommandSerializer _commandSerializer;

        private readonly Dictionary<int, IActionContainer> _registeredRecievers;

        public CommandRecieveService(Riptide.Client client, ICommandSerializer commandSerializer)
        {
            _commandSerializer = commandSerializer;
            _client = client;

            _client.MessageReceived += OnMessageRecieved;
            _registeredRecievers = new Dictionary<int, IActionContainer>();
        }

        private void OnMessageRecieved(object? _, MessageReceivedEventArgs e)
        {
            if (_registeredRecievers.TryGetValue(e.MessageId, out var callback))
                callback.Invoke(e.Message);
        }

        public void RegisterReciever<T>(Action<T> reciever) where T : unmanaged
        {
            int id = CommandIds.GetId<T>();
            ActionContainer<T> genericContainer;
            if (!_registeredRecievers.TryGetValue(id, out var container))
                _registeredRecievers.Add(id, genericContainer = new ActionContainer<T>(_commandSerializer));
            else
                genericContainer = (ActionContainer<T>)container;

            genericContainer.Recievers += reciever;
        }

        public void UnregisterReciever<T>(Action<T> reciever) where T : unmanaged
        {
            int id = CommandIds.GetId<T>();
            if (!_registeredRecievers.TryGetValue(id, out var container))
                return;

            ActionContainer<T> genericContainer = (ActionContainer<T>)container;
            genericContainer.Recievers -= reciever;
            if (!genericContainer.HasTargets)
                _registeredRecievers.Remove(id);
        }

        private class ActionContainer<T> : IActionContainer where T : unmanaged
        {
            public event Action<T>? Recievers;
            public bool HasTargets => Recievers?.GetInvocationList().Length > 0;
            private byte[] _tempBytes = Array.Empty<byte>();
            private readonly ICommandSerializer _commandSerializer;

            public ActionContainer(ICommandSerializer commandSerializer)
            {
                _commandSerializer = commandSerializer;
            }

            public void Invoke(Message m)
            {
                var data = GetData(m);
                Recievers?.Invoke(data);
            }

            private T GetData(Message message)
            {
                var length = (int)message.GetVarULong();
                if (_tempBytes.Length < length)
                    Array.Resize(ref _tempBytes, length);
                message.GetBytes(length, _tempBytes);
                return _commandSerializer.Deserialize<T>(_tempBytes.AsSpan(0, length));
            }
        }

        private interface IActionContainer
        {
            void Invoke(Message message);
        }
    }
}
